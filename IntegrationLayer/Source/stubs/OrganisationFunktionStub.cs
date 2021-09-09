using IntegrationLayer.OrganisationFunktion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class OrganisationFunktionStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrganisationFunktionStubHelper helper = new OrganisationFunktionStubHelper();
        private OrganisationRegistryProperties registry = OrganisationRegistryProperties.GetInstance();

        public void Importer(OrgFunctionData orgFunction)
        {
            // create ShortKey and Uuid if not supplied
            EnsureKeys(orgFunction);

            log.Debug("Attempting Import on OrgFunction with uuid " + orgFunction.Uuid);

            // create timestamp object to be used on all registrations, properties and relations
            VirkningType virkning = helper.GetVirkning(orgFunction.Timestamp);

            // for positions, it is possible to supply a custom start/stop date
            VirkningType unitRelationVirkning = virkning;
            if (orgFunction.FunctionTypeUuid.Equals(UUIDConstants.ORGFUN_POSITION) && !string.IsNullOrEmpty(orgFunction.StartDate))
            {
                unitRelationVirkning = helper.GetVirkning(orgFunction.StartDate, orgFunction.StopDate);
            }

            // setup registration
            RegistreringType1 registration = helper.CreateRegistration(orgFunction.Timestamp, LivscyklusKodeType.Importeret);

            // add properties
            helper.AddProperties(orgFunction.ShortKey, orgFunction.Name, virkning, registration);

            // add relationships on registration
            helper.AddTilknyttedeBrugere(orgFunction.Users, unitRelationVirkning, registration);
            helper.AddTilknyttedeEnheder(orgFunction.OrgUnits, unitRelationVirkning, registration);
            helper.AddTilknyttedeItSystemer(orgFunction.ItSystems, virkning, registration);
            helper.AddOpgaver(orgFunction.Tasks, virkning, registration);
            helper.AddOrganisationRelation(StubUtil.GetMunicipalityOrganisationUUID(), virkning, registration);
            helper.AddAddressReferences(orgFunction.Addresses, virkning, registration);
            helper.SetFunktionsType(orgFunction.FunctionTypeUuid, virkning, registration);

            // set Tilstand to Active
            helper.SetTilstandToActive(virkning, registration, orgFunction.Timestamp);

            // wire everything together
            OrganisationFunktionType organisationFunktionType = helper.GetOrganisationFunktionType(orgFunction.Uuid, registration);
            ImportInputType importInput = new ImportInputType();
            importInput.OrganisationFunktion = organisationFunktionType;

            // construct request
            importerRequest request = new importerRequest();
            request.ImporterRequest1 = new ImporterRequestType();
            request.ImporterRequest1.ImportInput = importInput;
            request.ImporterRequest1.AuthorityContext = new AuthorityContextType();
            request.ImporterRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send request
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Import", helper.CreatePort());

            try
            {
                importerResponse result = channel.importer(request);

                int statusCode = Int32.Parse(result.ImporterResponse1.ImportOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Import", OrganisationFunktionStubHelper.SERVICE, result.ImporterResponse1.ImportOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Import successful on OrgFunction with uuid " + orgFunction.Uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on OrganisationFunktion", ex);
            }
        }

        public void Ret(OrgFunctionData orgFunction, UpdateIndicator userIndicator, UpdateIndicator unitIndicator, UpdateIndicator taskIndicator, bool futurePositions = false)
        {
            log.Debug("Attempting Ret on OrganisationFunction with uuid " + orgFunction.Uuid);

            RegistreringType1 registration = GetLatestRegistration(orgFunction.Uuid, futurePositions);
            if (registration == null)
            {
                log.Debug("Cannot call Ret on OrganisationFunktion with uuid " + orgFunction.Uuid + " because it does not exist in Organisation");
                return;
            }

            VirkningType virkning = helper.GetVirkning(orgFunction.Timestamp);

            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                bool changes = false;

                RetInputType1 input = new RetInputType1();
                input.UUIDIdentifikator = orgFunction.Uuid;
                input.AttributListe = registration.AttributListe;
                input.TilstandListe = registration.TilstandListe;
                input.RelationListe = registration.RelationListe;

                changes = helper.SetTilstandToActive(virkning, registration, orgFunction.Timestamp) | changes;

                #region Update attributes if needed
                EgenskabType latestProperty = StubUtil.GetLatestProperty(input.AttributListe.Egenskab);
                if (latestProperty == null || latestProperty.FunktionNavn == null ||
                   (orgFunction.Name != null && !latestProperty.FunktionNavn.Equals(orgFunction.Name)) ||
                   (orgFunction.ShortKey != null && !latestProperty.BrugervendtNoegleTekst.Equals(orgFunction.ShortKey)))
                {
                    if (latestProperty == null)
                    {
                        orgFunction.ShortKey = (orgFunction.ShortKey != null) ? orgFunction.ShortKey : IdUtil.GenerateShortKey();

                        // special case where editing a function that has been orphaned, without supplying a name - should never really happen, but the API allows it
                        orgFunction.Name = (orgFunction.Name != null) ? orgFunction.Name : "Unknown Function";
                    }

                    // create a new property
                    EgenskabType newProperty = new EgenskabType();
                    newProperty.Virkning = helper.GetVirkning(orgFunction.Timestamp);
                    newProperty.BrugervendtNoegleTekst = (orgFunction.ShortKey != null) ? orgFunction.ShortKey : latestProperty.BrugervendtNoegleTekst;
                    newProperty.FunktionNavn = (orgFunction.Name != null) ? orgFunction.Name : latestProperty.FunktionNavn;

                    // create a new set of properties
                    input.AttributListe.Egenskab = new EgenskabType[1];
                    input.AttributListe.Egenskab[0] = newProperty;

                    changes = true;
                }
                #endregion

                #region update tasks if needed
                if (taskIndicator.Equals(UpdateIndicator.COMPARE))
                {
                    // terminate the Virkning on all address relationships that no longer exists locally
                    changes = StubUtil.TerminateObjectsInOrgNoLongerPresentLocally(input.RelationListe.Opgaver, orgFunction.Tasks, orgFunction.Timestamp, true) || changes;

                    // add references to address objects that are new
                    List<string> taskUuidsToAdd = StubUtil.FindAllObjectsInLocalNotInOrg(input.RelationListe.Opgaver, orgFunction.Tasks, true);

                    if (taskUuidsToAdd.Count > 0)
                    {
                        int size = taskUuidsToAdd.Count + ((input.RelationListe.Opgaver != null) ? input.RelationListe.Opgaver.Length : 0);
                        KlasseFlerRelationType[] newTasks = new KlasseFlerRelationType[size];

                        int i = 0;
                        if (input.RelationListe.Opgaver != null)
                        {
                            foreach (var taskInOrg in input.RelationListe.Opgaver)
                            {
                                newTasks[i++] = taskInOrg;
                            }
                        }

                        foreach (string uuidToAdd in taskUuidsToAdd)
                        {
                            foreach (var taskInLocal in orgFunction.Tasks)
                            {
                                if (taskInLocal.Equals(uuidToAdd))
                                {
                                    KlasseFlerRelationType newTask = helper.CreateOpgaveRelation(uuidToAdd, virkning);
                                    newTasks[i++] = newTask;
                                }
                            }
                        }

                        input.RelationListe.Opgaver = newTasks;
                        changes = true;
                    }
                }
                #endregion

                #region Update TilknyttedeBrugere relationships
                // terminate references
                if (userIndicator.Equals(UpdateIndicator.NONE))
                {
                    ; // do nothing
                }
                else if (userIndicator.Equals(UpdateIndicator.COMPARE))
                {
                    // terminate the references in Org that no longer exist locally
                    changes = StubUtil.TerminateObjectsInOrgNoLongerPresentLocally(input.RelationListe.TilknyttedeBrugere, orgFunction.Users, orgFunction.Timestamp, false) || changes;

                    VirkningType unitRelationVirkning = virkning;
                    if (orgFunction.FunctionTypeUuid.Equals(UUIDConstants.ORGFUN_POSITION) && !string.IsNullOrEmpty(orgFunction.StartDate))
                    {
                        unitRelationVirkning = helper.GetVirkning(orgFunction.StartDate, orgFunction.StopDate);
                    }

                    List<string> uuidsToAdd = new List<string>();
                    foreach (string userUuid in orgFunction.Users)
                    {
                        bool found = false;
                        if (input.RelationListe.TilknyttedeBrugere != null)                        {
                            foreach (var tilknyttetBruger in input.RelationListe.TilknyttedeBrugere)
                            {
                                string tilknyttetBrugerUuid = tilknyttetBruger.ReferenceID?.Item;
                                if (userUuid.Equals(tilknyttetBrugerUuid))
                                {
                                    found = true;
                                    // detect changes in start/stop dates if values are supplied
                                    if (orgFunction.FunctionTypeUuid.Equals(UUIDConstants.ORGFUN_POSITION) && !string.IsNullOrEmpty(orgFunction.StartDate))
                                    {
                                        string existingUnitStartDate = null, existingUnitStopDate = null;
                                        if (tilknyttetBruger.Virkning?.FraTidspunkt?.Item is DateTime)
                                        {
                                            existingUnitStartDate = ((DateTime)tilknyttetBruger.Virkning.FraTidspunkt.Item).ToString("yyyy-MM-dd");
                                        }
                                        if (tilknyttetBruger.Virkning?.TilTidspunkt?.Item is DateTime)
                                        {
                                            existingUnitStopDate = ((DateTime)tilknyttetBruger.Virkning.TilTidspunkt.Item).ToString("yyyy-MM-dd");
                                        }
                                        if (!string.Equals(orgFunction.StartDate, existingUnitStartDate) || !string.Equals(orgFunction.StopDate, existingUnitStopDate))
                                        {
                                            tilknyttetBruger.Virkning = unitRelationVirkning;
                                            changes = true;
                                        }
                                    }
                                }
                            }
                        }
                        // add this one
                        if (!found)
                        {
                            uuidsToAdd.Add(userUuid);
                        }
                    }

                    // add all the new references
                    if (uuidsToAdd.Count > 0)
                    {
                        int size = uuidsToAdd.Count + ((input.RelationListe.TilknyttedeBrugere != null) ? input.RelationListe.TilknyttedeBrugere.Length : 0);
                        BrugerFlerRelationType[] newUsers = new BrugerFlerRelationType[size];

                        int i = 0;
                        if (input.RelationListe.TilknyttedeBrugere != null)
                        {
                            foreach (var usersInOrg in input.RelationListe.TilknyttedeBrugere)
                            {
                                newUsers[i++] = usersInOrg;
                            }
                        }

                        foreach (string uuidToAdd in uuidsToAdd)
                        {
                            newUsers[i++] = helper.CreateBrugerRelation(uuidToAdd, unitRelationVirkning);
                        }

                        input.RelationListe.TilknyttedeBrugere = newUsers;
                        changes = true;
                    }
                }
                #endregion

                #region Update TilknyttedeEnheder relationships

                if (unitIndicator.Equals(UpdateIndicator.NONE))
                {
                    ; // do nothing
                }
                else if (unitIndicator.Equals(UpdateIndicator.COMPARE))
                {
                    // terminate the references in Org that no longer exist locally
                    changes = StubUtil.TerminateObjectsInOrgNoLongerPresentLocally(input.RelationListe.TilknyttedeEnheder, orgFunction.OrgUnits, orgFunction.Timestamp, false) || changes;

                    VirkningType unitRelationVirkning = virkning;
                    if (orgFunction.FunctionTypeUuid.Equals(UUIDConstants.ORGFUN_POSITION) && !string.IsNullOrEmpty(orgFunction.StartDate))
                    {
                        unitRelationVirkning = helper.GetVirkning(orgFunction.StartDate, orgFunction.StopDate);
                    }

                    // find those to update or add
                    List<string> uuidsToAdd = new List<string>();
                    foreach (string ouUuid in orgFunction.OrgUnits)
                    {
                        bool found = false;

                        if (input.RelationListe.TilknyttedeEnheder != null)
                        {
                            foreach (var tilknyttetEnhed in input.RelationListe.TilknyttedeEnheder)
                            {
                                string tilknyttetEnhedUuid = tilknyttetEnhed.ReferenceID?.Item;

                                if (ouUuid.Equals(tilknyttetEnhedUuid))
                                {
                                    found = true;

                                    // detect changes in start/stop dates if values are supplied
                                    if (orgFunction.FunctionTypeUuid.Equals(UUIDConstants.ORGFUN_POSITION) && !string.IsNullOrEmpty(orgFunction.StartDate))
                                    {
                                        string existingUnitStartDate = null, existingUnitStopDate = null;
                                        if (tilknyttetEnhed.Virkning?.FraTidspunkt?.Item is DateTime)
                                        {
                                            existingUnitStartDate = ((DateTime)tilknyttetEnhed.Virkning.FraTidspunkt.Item).ToString("yyyy-MM-dd");
                                        }
                                        if (tilknyttetEnhed.Virkning?.TilTidspunkt?.Item is DateTime)
                                        {
                                            existingUnitStopDate = ((DateTime)tilknyttetEnhed.Virkning.TilTidspunkt.Item).ToString("yyyy-MM-dd");
                                        }

                                        if (!string.Equals(orgFunction.StartDate, existingUnitStartDate) || !string.Equals(orgFunction.StopDate, existingUnitStopDate))
                                        {
                                            tilknyttetEnhed.Virkning = unitRelationVirkning;
                                            changes = true;
                                        }
                                    }
                                }
                            }
                        }

                        // add this one
                        if (!found)
                        {
                            uuidsToAdd.Add(ouUuid);
                        }
                    }

                    // add all the new references
                    if (uuidsToAdd.Count > 0)
                    {
                        int size = uuidsToAdd.Count + ((input.RelationListe.TilknyttedeEnheder != null) ? input.RelationListe.TilknyttedeEnheder.Length : 0);
                        OrganisationEnhedFlerRelationType[] newUnits = new OrganisationEnhedFlerRelationType[size];

                        int i = 0;
                        if (input.RelationListe.TilknyttedeEnheder != null)
                        {
                            foreach (var unit in input.RelationListe.TilknyttedeEnheder)
                            {
                                newUnits[i++] = unit;
                            }
                        }

                        foreach (string uuidToAdd in uuidsToAdd)
                        {
                            newUnits[i++] = helper.CreateOrgEnhedRelation(uuidToAdd, unitRelationVirkning);
                        }

                        input.RelationListe.TilknyttedeEnheder = newUnits;
                        changes = true;
                    }
                }
                else
                {
                    log.Error("Unsupported updateIndicator for enhedsrelationer: " + unitIndicator.ToString());
                }
                #endregion

                #region Update organisation relationship
                bool foundExistingValidOrganisationRelation = false;
                if (registration.RelationListe.TilknyttedeOrganisationer != null && registration.RelationListe.TilknyttedeOrganisationer.Length > 0)
                {
                    foreach (OrganisationFlerRelationType orgRelation in registration.RelationListe.TilknyttedeOrganisationer)
                    {
                        // make sure that the pointer is set correctly
                        if (!StubUtil.GetMunicipalityOrganisationUUID().Equals(orgRelation.ReferenceID.Item))
                        {
                            orgRelation.ReferenceID.Item = StubUtil.GetMunicipalityOrganisationUUID();
                            changes = true;
                        }

                        // update the Virkning on the TilknyttedeOrganisationer relationship if needed (undelete feature)
                        object endTime = orgRelation.Virkning.TilTidspunkt.Item;

                        // endTime is bool => ok
                        // endTime is DateTime, but Now is before endTime => ok
                        if (!(endTime is DateTime) || (DateTime.Compare(DateTime.Now, (DateTime)endTime) < 0))
                        {
                            foundExistingValidOrganisationRelation = true;
                        }
                    }
                }

                if (!foundExistingValidOrganisationRelation)
                {
                    helper.AddOrganisationRelation(StubUtil.GetMunicipalityOrganisationUUID(), virkning, registration);
                    changes = true;
                }
                #endregion

                // TODO: addresses are not currently used for functions, this is a left-over from the days of it-systems and JumpUrls
                #region Update Address relationships
                // terminate the Virkning on all address relationships that no longer exists locally
                changes = StubUtil.TerminateObjectsInOrgNoLongerPresentLocally(input.RelationListe.Adresser, orgFunction.Addresses, orgFunction.Timestamp, true) || changes;

                // add references to address objects that are new
                List<string> addressUuidsToAdd = StubUtil.FindAllObjectsInLocalNotInOrg(input.RelationListe.Adresser, orgFunction.Addresses, true);

                if (addressUuidsToAdd.Count > 0)
                {
                    int size = addressUuidsToAdd.Count + ((input.RelationListe.Adresser != null) ? input.RelationListe.Adresser.Length : 0);
                    AdresseFlerRelationType[] newAdresser = new AdresseFlerRelationType[size];

                    int i = 0;
                    if (input.RelationListe.Adresser != null)
                    {
                        foreach (var addressInOrg in input.RelationListe.Adresser)
                        {
                            newAdresser[i++] = addressInOrg;
                        }
                    }

                    foreach (string uuidToAdd in addressUuidsToAdd)
                    {
                        foreach (var addressInLocal in orgFunction.Addresses)
                        {
                            if (addressInLocal.Uuid.Equals(uuidToAdd))
                            {
                                string roleUuid = null;
                                switch (addressInLocal.Type)
                                {
                                    case AddressRelationType.URL:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGFUNCTION_URL;
                                        break;
                                    default:
                                        log.Warn("Cannot add relationship to address of type " + addressInLocal.Type + " with uuid " + addressInLocal.Uuid + " as the type is unknown");
                                        continue;
                                }

                                AdresseFlerRelationType newAddress = helper.CreateAddressReference(uuidToAdd, (i + 1), roleUuid, virkning);
                                newAdresser[i++] = newAddress;
                            }
                        }
                    }

                    input.RelationListe.Adresser = newAdresser;
                    changes = true;
                }
                #endregion

                // if no changes are made, we do not call the service
                if (!changes)
                {
                    log.Debug("Ret on OrganisationFunktion with uuid " + orgFunction.Uuid + " cancelled because of no changes");
                    return;
                }

                // send Ret request
                retRequest request = new retRequest();
                request.RetRequest1 = new RetRequestType();
                request.RetRequest1.RetInput = input;
                request.RetRequest1.AuthorityContext = new AuthorityContextType();
                request.RetRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

                retResponse response = channel.ret(request);

                int statusCode = Int32.Parse(response.RetResponse1.RetOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49)
                    {
                        log.Warn("Ret failed on OrgFunction " + orgFunction.Uuid + " as Organisation returned status 49. The most likely cause is that the object has been Passiveret");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", OrganisationFunktionStubHelper.SERVICE, response.RetResponse1.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Ret succesful on OrganisationFunktion with uuid " + orgFunction.Uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on OrganisationFunktion", ex);
            }
        }

/* these two methods are the good ones, but we cannot use them because of a bug in search
        public List<FiltreretOejebliksbilledeType> SoegAndGetLatestRegistration(string functionsTypeUuid, string userUuid, string unitUuid, string itSystemUuid)
        {
            List<FiltreretOejebliksbilledeType> result = new List<FiltreretOejebliksbilledeType>();

            // perform a search and then retrieve all the objects that matches the search criteria
            List<string> uuidCandidates = Soeg(functionsTypeUuid, userUuid, unitUuid, itSystemUuid);
            if (uuidCandidates == null || uuidCandidates.Count == 0)
            {
                return result;
            }

            FiltreretOejebliksbilledeType[] resultCandidates = GetLatestRegistrations(uuidCandidates.ToArray());
            if (resultCandidates == null || resultCandidates.Length == 0)
            {
                return result;
            }

            foreach (FiltreretOejebliksbilledeType resultCandidate in resultCandidates)
            {
                result.Add(resultCandidate);
            }

            return result;
        }

        public List<string> SoegAndGetUuids(string functionsTypeUuid, string userUuid, string unitUuid, string itSystemUuid)
        {
            List<string> uuids = Soeg(functionsTypeUuid, userUuid, unitUuid, itSystemUuid);
            if (uuids == null)
            {
                return new List<string>();
            }

            return uuids;
        }
*/

        /* these two are the bad ones, but they contain the workaround for the bug on search */
        public List<FiltreretOejebliksbilledeType> SoegAndGetLatestRegistration(string functionsTypeUuid, string userUuid, string unitUuid, string itSystemUuid)
        {
            List<FiltreretOejebliksbilledeType> result = new List<FiltreretOejebliksbilledeType>();

            // perform a search and then retrieve all the objects that matches the search criteria
            List<string> uuidCandidates = Soeg(functionsTypeUuid, userUuid, unitUuid, itSystemUuid);
            if (uuidCandidates == null || uuidCandidates.Count == 0)
            {
                return result;
            }

            FiltreretOejebliksbilledeType[] resultCandidates = GetLatestRegistrations(uuidCandidates.ToArray(), string.Equals(UUIDConstants.ORGFUN_POSITION, functionsTypeUuid));
            if (resultCandidates == null || resultCandidates.Length == 0)
            {
                return result;
            }

            foreach (FiltreretOejebliksbilledeType resultCandidate in resultCandidates)
            {
                if (resultCandidate.Registrering == null || resultCandidate.Registrering.Length == 0)
                {
                    log.Warn("Result candidate with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " does not have a registration - it is skipped in the result");
                    continue;
                }

                if (resultCandidate.Registrering.Length > 1)
                {
                    log.Warn("Result candidate with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " has more than one registration - it is skipped in the result");
                    continue;
                }

                RegistreringType1 registration = resultCandidate.Registrering[0];

                if (userUuid != null)
                {
                    bool found = false;

                    if (registration.RelationListe.TilknyttedeBrugere != null)
                    {
                        foreach (var userRelation in registration.RelationListe.TilknyttedeBrugere)
                        {
                            if (userUuid.Equals(userRelation.ReferenceID?.Item))
                            {
                                found = true;
                            }

                        }
                    }

                    if (!found)
                    {
                        log.Debug("Filtering OrgFunction with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " because it does not have a correct user relation");
                        continue;
                    }
                }

                if (unitUuid != null)
                {
                    bool found = false;

                    if (registration.RelationListe.TilknyttedeEnheder != null)
                    {
                        foreach (var unitRelation in registration.RelationListe.TilknyttedeEnheder)
                        {
                            if (unitUuid.Equals(unitRelation.ReferenceID?.Item))
                            {
                                found = true;
                            }

                        }
                    }

                    if (!found)
                    {
                        log.Debug("Filtering OrgFunction with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " because it does not have a correct unit relation");
                        continue;
                    }
                }

                if (itSystemUuid != null)
                {
                    bool found = false;

                    if (registration.RelationListe.TilknyttedeItSystemer != null)
                    {
                        foreach (var itSystemRelation in registration.RelationListe.TilknyttedeItSystemer)
                        {
                            if (itSystemUuid.Equals(itSystemRelation.ReferenceID?.Item))
                            {
                                found = true;
                            }

                        }
                    }

                    if (!found)
                    {
                        log.Debug("Filtering OrgFunction with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " because it does not have a correct itSystem relation");
                        continue;
                    }
                }

                result.Add(resultCandidate);
            }

            return result;
        }

        public List<string> SoegAndGetUuids(string functionsTypeUuid, string userUuid, string unitUuid, string itSystemUuid)
        {
            List<string> result = new List<string>();

            // perform a search and then retrieve all the objects that matches the search criteria
            List<string> uuidCandidates = Soeg(functionsTypeUuid, userUuid, unitUuid, itSystemUuid);
            if (uuidCandidates == null || uuidCandidates.Count == 0)
            {
                return result;
            }

            FiltreretOejebliksbilledeType[] resultCandidates = GetLatestRegistrations(uuidCandidates.ToArray());
            if (resultCandidates == null || resultCandidates.Length == 0)
            {
                return result;
            }

            // check that each result from the search actually matches the search criteria (because KMDs implementation doesn't do it corectly :( )
            foreach (FiltreretOejebliksbilledeType resultCandidate in resultCandidates)
            {
                if (resultCandidate.Registrering == null || resultCandidate.Registrering.Length == 0)
                {
                    log.Warn("Result candidate with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " does not have a registration - it is skipped in the result");
                    continue;
                }

                if (resultCandidate.Registrering.Length > 1)
                {
                    log.Warn("Result candidate with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " has more than one registration - it is skipped in the result");
                    continue;
                }

                RegistreringType1 registration = resultCandidate.Registrering[0];

                if (userUuid != null)
                {
                    bool found = false;

                    if (registration.RelationListe.TilknyttedeBrugere != null)
                    {
                        foreach (var userRelation in registration.RelationListe.TilknyttedeBrugere)
                        {
                            if (userUuid.Equals(userRelation.ReferenceID?.Item))
                            {
                                found = true;
                            }

                        }
                    }

                    if (!found)
                    {
                        log.Debug("Filtering OrgFunction with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " because it does not have a correct user relation");
                        continue;
                    }
                }

                if (unitUuid != null)
                {
                    bool found = false;

                    if (registration.RelationListe.TilknyttedeEnheder != null)
                    {
                        foreach (var unitRelation in registration.RelationListe.TilknyttedeEnheder)
                        {
                            if (unitUuid.Equals(unitRelation.ReferenceID?.Item))
                            {
                                found = true;
                            }

                        }
                    }

                    if (!found)
                    {
                        log.Debug("Filtering OrgFunction with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " because it does not have a correct unit relation");
                        continue;
                    }
                }

                if (itSystemUuid != null)
                {
                    bool found = false;

                    if (registration.RelationListe.TilknyttedeItSystemer != null)
                    {
                        foreach (var itSystemRelation in registration.RelationListe.TilknyttedeItSystemer)
                        {
                            if (itSystemUuid.Equals(itSystemRelation.ReferenceID?.Item))
                            {
                                found = true;
                            }

                        }
                    }

                    if (!found)
                    {
                        log.Debug("Filtering OrgFunction with uuid " + resultCandidate.ObjektType.UUIDIdentifikator + " because it does not have a correct itSystem relation");
                        continue;
                    }
                }

                result.Add(resultCandidate.ObjektType.UUIDIdentifikator);
            }

            return result;
        }

        private List<string> Soeg(string functionsTypeUuid, string userUuid, string unitUuid, string itSystemUuid)
        {
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Soeg", helper.CreatePort());

            SoegInputType1 soegInput = new SoegInputType1();
            soegInput.AttributListe = new AttributListeType();
            soegInput.RelationListe = new RelationListeType();
            soegInput.TilstandListe = new TilstandListeType();

            // only return objects that have a Tilhører relationship top-level Organisation
            UnikIdType orgReference = StubUtil.GetReference<UnikIdType>(registry.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()], ItemChoiceType.UUIDIdentifikator);
            soegInput.RelationListe.TilknyttedeOrganisationer = new OrganisationFlerRelationType[1];
            soegInput.RelationListe.TilknyttedeOrganisationer[0] = new OrganisationFlerRelationType();
            soegInput.RelationListe.TilknyttedeOrganisationer[0].ReferenceID = orgReference;

            if (!String.IsNullOrEmpty(functionsTypeUuid))
            {
                UnikIdType reference = new UnikIdType();
                reference.Item = functionsTypeUuid;
                reference.ItemElementName = ItemChoiceType.UUIDIdentifikator;

                KlasseRelationType funktionsType = new KlasseRelationType();
                funktionsType.ReferenceID = reference;
                soegInput.RelationListe.Funktionstype = funktionsType;
            }

            if (!String.IsNullOrEmpty(userUuid))
            {
                UnikIdType reference = new UnikIdType();
                reference.Item = userUuid;
                reference.ItemElementName = ItemChoiceType.UUIDIdentifikator;

                soegInput.RelationListe.TilknyttedeBrugere = new BrugerFlerRelationType[1];
                soegInput.RelationListe.TilknyttedeBrugere[0] = new BrugerFlerRelationType();
                soegInput.RelationListe.TilknyttedeBrugere[0].ReferenceID = reference;

                // when, and only when, we search for positions, we also want future positions because
                // we know care about start/stop dates ;)
                if (string.Equals(functionsTypeUuid, UUIDConstants.ORGFUN_POSITION))
                {
                    soegInput.RelationListe.TilknyttedeBrugere[0].Virkning = new VirkningType();
                    soegInput.RelationListe.TilknyttedeBrugere[0].Virkning.FraTidspunkt = new TidspunktType();
                    soegInput.RelationListe.TilknyttedeBrugere[0].Virkning.FraTidspunkt.Item = DateTime.Now;
                    soegInput.RelationListe.TilknyttedeBrugere[0].Virkning.FraTidspunkt = new TidspunktType();
                    soegInput.RelationListe.TilknyttedeBrugere[0].Virkning.FraTidspunkt.Item = true;
                }
            }

            if (!String.IsNullOrEmpty(unitUuid))
            {
                UnikIdType reference = new UnikIdType();
                reference.Item = unitUuid;
                reference.ItemElementName = ItemChoiceType.UUIDIdentifikator;

                soegInput.RelationListe.TilknyttedeEnheder = new OrganisationEnhedFlerRelationType[1];
                soegInput.RelationListe.TilknyttedeEnheder[0] = new OrganisationEnhedFlerRelationType();
                soegInput.RelationListe.TilknyttedeEnheder[0].ReferenceID = reference;

                // when, and only when, we search for positions, we also want future positions because
                // we know care about start/stop dates ;)
                if (string.Equals(functionsTypeUuid, UUIDConstants.ORGFUN_POSITION))
                {
                    soegInput.RelationListe.TilknyttedeEnheder[0].Virkning = new VirkningType();
                    soegInput.RelationListe.TilknyttedeEnheder[0].Virkning.FraTidspunkt = new TidspunktType();
                    soegInput.RelationListe.TilknyttedeEnheder[0].Virkning.FraTidspunkt.Item = DateTime.Now;
                    soegInput.RelationListe.TilknyttedeEnheder[0].Virkning.FraTidspunkt = new TidspunktType();
                    soegInput.RelationListe.TilknyttedeEnheder[0].Virkning.FraTidspunkt.Item = true;
                }
            }

            if (!String.IsNullOrEmpty(itSystemUuid))
            {
                UnikIdType reference = new UnikIdType();
                reference.Item = itSystemUuid;
                reference.ItemElementName = ItemChoiceType.UUIDIdentifikator;

                soegInput.RelationListe.TilknyttedeItSystemer = new ItSystemFlerRelationType[1];
                soegInput.RelationListe.TilknyttedeItSystemer[0] = new ItSystemFlerRelationType();
                soegInput.RelationListe.TilknyttedeItSystemer[0].ReferenceID = reference;
            }

            // search
            soegRequest request = new soegRequest();
            request.SoegRequest1 = new SoegRequestType();
            request.SoegRequest1.SoegInput = soegInput;
            request.SoegRequest1.AuthorityContext = new AuthorityContextType();
            request.SoegRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            try
            {
                soegResponse response = channel.soeg(request);
                int statusCode = Int32.Parse(response.SoegResponse1.SoegOutput.StandardRetur.StatusKode);
                if (statusCode != 20 && statusCode != 44) // 44 is empty search result
                {
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Soeg", OrganisationFunktionStubHelper.SERVICE, response.SoegResponse1.SoegOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                List<string> functions = new List<string>();
                if (statusCode == 20)
                {
                    foreach (string id in response.SoegResponse1.SoegOutput.IdListe)
                    {
                        functions.Add(id);
                    }
                }

                return functions;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Soeg service on OrganisationFunktion", ex);
            }
        }

        public void Deactivate(string uuid, DateTime timestamp, bool futurePositions = false)
        {
            log.Debug("Attempting Deactivate on OrganisationFunktion with uuid " + uuid);

            RegistreringType1 registration = GetLatestRegistration(uuid, futurePositions);
            if (registration == null)
            {
                log.Debug("Cannot call Deactivate on OrganisationFunktion with uuid " + uuid + " because it does not exist in Organisation");
                return;
            }

            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                RetInputType1 input = new RetInputType1();
                input.UUIDIdentifikator = uuid;
                input.AttributListe = registration.AttributListe;
                input.TilstandListe = registration.TilstandListe;
                input.RelationListe = registration.RelationListe;

                // cut relationship to all users
                if (input.RelationListe.TilknyttedeBrugere != null && input.RelationListe.TilknyttedeBrugere.Length > 0)
                {
                    foreach (var bruger in input.RelationListe.TilknyttedeBrugere)
                    {
                        StubUtil.TerminateVirkning(bruger.Virkning, timestamp, futurePositions);
                    }
                }

                // cut relationship to all orgUnits
                if (input.RelationListe.TilknyttedeEnheder != null && input.RelationListe.TilknyttedeEnheder.Length > 0)
                {
                    foreach (var enhed in input.RelationListe.TilknyttedeEnheder)
                    {
                        StubUtil.TerminateVirkning(enhed.Virkning, timestamp, futurePositions);
                    }
                }

                // cut relationship to all Opgaver
                if (input.RelationListe.Opgaver != null && input.RelationListe.Opgaver.Length > 0)
                {
                    foreach (var opgave in input.RelationListe.Opgaver)
                    {
                        StubUtil.TerminateVirkning(opgave.Virkning, timestamp);
                    }
                }

                // actually deactivate function
                VirkningType virkning = helper.GetVirkning(timestamp);
                helper.SetTilstandToInactive(virkning, registration, timestamp);

                retRequest request = new retRequest();
                request.RetRequest1 = new RetRequestType();
                request.RetRequest1.RetInput = input;
                request.RetRequest1.AuthorityContext = new AuthorityContextType();
                request.RetRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

                retResponse response = channel.ret(request);

                int statusCode = Int32.Parse(response.RetResponse1.RetOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", OrganisationFunktionStubHelper.SERVICE, response.RetResponse1.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Deactivate on OrganisationFunktion with uuid " + uuid + " succeded");
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on OrganisationFunktion", ex);
            }
        }

        public RegistreringType1 GetLatestRegistration(string uuid, bool futurePositions = false)
        {
            FiltreretOejebliksbilledeType[] registrations = GetLatestRegistrations(new string[] { uuid }, futurePositions);
            if (registrations == null || registrations.Length == 0)
            {
                return null;
            }

            RegistreringType1[] resultSet = registrations[0].Registrering;
            if (resultSet.Length == 0)
            {
                log.Warn("OrgFunction with uuid '" + uuid + "' exists, but has no registration");
                return null;
            }

            RegistreringType1 result = null;
            if (resultSet.Length > 1)
            {
                log.Warn("OrgFunction with uuid " + uuid + " has more than one registration when reading latest registration, this should never happen");

                DateTime winner = DateTime.MinValue;
                foreach (RegistreringType1 res in resultSet)
                {
                    // first time through will always result in a True evaluation here
                    if (DateTime.Compare(winner, res.Tidspunkt) < 0)
                    {
                        result = res;
                        winner = res.Tidspunkt;
                    }
                }
            }
            else
            {
                result = resultSet[0];
            }

            // we cannot perform any kind of updates on Slettet/Passiveret, så it makes sense to filter them out on lookup,
            // so the rest of the code will default to Import op top of this
            if (result.LivscyklusKode.Equals(LivscyklusKodeType.Slettet) || result.LivscyklusKode.Equals(LivscyklusKodeType.Passiveret))
            {
                return null;
            }

            return result;
        }

        public FiltreretOejebliksbilledeType[] GetLatestRegistrations(string[] uuids, bool futurePositions = false)
        {
            ListInputType listInput = new ListInputType();
            listInput.UUIDIdentifikator = uuids;

            listRequest request = new listRequest();
            request.ListRequest1 = new ListRequestType();
            request.ListRequest1.ListInput = listInput;
            request.ListRequest1.AuthorityContext = new AuthorityContextType();
            request.ListRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            if (futurePositions)
            {
                request.ListRequest1.ListInput.VirkningFraFilter = new TidspunktType();
                request.ListRequest1.ListInput.VirkningFraFilter.Item = DateTime.Now;
                request.ListRequest1.ListInput.VirkningTilFilter = new TidspunktType();
                request.ListRequest1.ListInput.VirkningTilFilter.Item = true;
            }

            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "List", helper.CreatePort());

            try
            {
                listResponse response = channel.list(request);

                int statusCode = Int32.Parse(response.ListResponse1.ListOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the objects does not exists, so that is a valid response
                    if (statusCode != 44)
                    {
                        log.Warn("Lookup on OrgFunction with uuids '" + string.Join(",", uuids) + "' failed with statuscode " + statusCode);
                    }
                    else
                    {
                        log.Debug("Lookup on OrgFunction with uuids '" + string.Join(",", uuids) + "' failed with statuscode " + statusCode);
                    }

                    return null;
                }

                if (response.ListResponse1.ListOutput.FiltreretOejebliksbillede == null || response.ListResponse1.ListOutput.FiltreretOejebliksbillede.Length == 0)
                {
                    log.Debug("Lookup on OrgFunction with uuids '" + string.Join(",", uuids) + "' returned an empty resultset");
                    return null;
                }

                return response.ListResponse1.ListOutput.FiltreretOejebliksbillede;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on OrganisationFunktion", ex);
            }
        }

        // this is a special version of the one found in StubUtil - it handles removing objects that are present in the input (so the reverse method basically ;))
        private bool TerminateObjectsInOrgThatAreInLocal(dynamic objectsInOrg, List<string> objectsInLocal, DateTime timestamp)
        {
            bool changes = false;

            if (objectsInOrg != null)
            {
                foreach (var objectInOrg in objectsInOrg)
                {
                    if (objectsInLocal != null)
                    {
                        foreach (var objectInLocal in objectsInLocal)
                        {
                            if (objectInLocal.Equals(objectInOrg.ReferenceID.Item))
                            {
                                // the objectsInOrg collection contains references that are already terminaited, so TerminateVirkning
                                // only returns true if it actually terminiated the reference - and we do not want to flag the object
                                // as modified unless it actually is
                                if (StubUtil.TerminateVirkning(objectInOrg.Virkning, timestamp))
                                {
                                    changes = true;
                                }
                            }
                        }
                    }
                }
            }

            return changes;
        }

        private void EnsureKeys(OrgFunctionData orgFunction)
        {
            orgFunction.Uuid = (orgFunction.Uuid != null) ? orgFunction.Uuid : IdUtil.GenerateUuid();
            orgFunction.ShortKey = (orgFunction.ShortKey != null) ? orgFunction.ShortKey : IdUtil.GenerateShortKey();
        }
    }
}
