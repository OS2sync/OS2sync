using IntegrationLayer.OrganisationEnhed;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class OrganisationEnhedStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrganisationEnhedStubHelper helper = new OrganisationEnhedStubHelper();
        private OrganisationRegistryProperties registry = OrganisationRegistryProperties.GetInstance();

        public void Importer(OrgUnitData unit)
        {
            log.Debug("Attempting Importer on OrganisationEnhed with uuid " + unit.Uuid);

            // create ShortKey if not supplied
            EnsureKeys(unit);

            // create timestamp object to be used on all registrations, properties and relations
            VirkningType virkning = helper.GetVirkning(unit.Timestamp);

            // setup registration
            RegistreringType1 registration = helper.CreateRegistration(unit, LivscyklusKodeType.Importeret);

            // add properties
            helper.AddProperties(unit.ShortKey, unit.Name, virkning, registration);

            // add relationships
            helper.SetType(unit.OrgUnitType, virkning, registration);
            helper.AddAddressReferences(unit.Addresses, virkning, registration);
            helper.AddOrganisationRelation(StubUtil.GetMunicipalityOrganisationUUID(), virkning, registration);
            helper.AddOverordnetEnhed(unit.ParentOrgUnitUuid, virkning, registration);
            helper.AddTilknyttedeFunktioner(unit.OrgFunctionUuids, virkning, registration);
            helper.AddOpgaver(unit.Tasks, virkning, registration);

            // set Tilstand to Active
            helper.SetTilstandToActive(virkning, registration, unit.Timestamp);

            // wire everything together
            OrganisationEnhedType organisationEnhedType = helper.GetOrganisationEnhedType(unit.Uuid, registration);
            ImportInputType importInput = new ImportInputType();
            importInput.OrganisationEnhed = organisationEnhedType;

            // construct request
            importerRequest request = new importerRequest();
            request.ImporterRequest1 = new ImporterRequestType();
            request.ImporterRequest1.ImportInput = importInput;
            request.ImporterRequest1.AuthorityContext = new AuthorityContextType();
            request.ImporterRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send request
            OrganisationEnhedPortType channel = StubUtil.CreateChannel<OrganisationEnhedPortType>(OrganisationEnhedStubHelper.SERVICE, "Importer", helper.CreatePort());

            try
            {
                importerResponse result = channel.importer(request);
                int statusCode = Int32.Parse(result.ImporterResponse1.ImportOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Import", OrganisationEnhedStubHelper.SERVICE, result.ImporterResponse1.ImportOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Importer successful on OrganisationEnhed with uuid " + unit.Uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on OrganisationEnhed", ex);
            }
        }

        public void Ret(OrgUnitData unit)
        {
            log.Debug("Attempting Ret on OrganisationEnhed with uuid " + unit.Uuid);

            RegistreringType1 registration = GetLatestRegistration(unit.Uuid);
            if (registration == null)
            {
                log.Debug("Cannot call Ret on OrganisationEnhed with uuid " + unit.Uuid + " because it does not exist in Organisation");
                return;
            }

            VirkningType virkning = helper.GetVirkning(unit.Timestamp);

            OrganisationEnhedPortType channel = StubUtil.CreateChannel<OrganisationEnhedPortType>(OrganisationEnhedStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                bool changes = false;

                RetInputType1 input = new RetInputType1();
                input.UUIDIdentifikator = unit.Uuid;
                input.AttributListe = registration.AttributListe;
                input.TilstandListe = registration.TilstandListe;
                input.RelationListe = registration.RelationListe;

                changes = helper.SetTilstandToActive(virkning, registration, unit.Timestamp) | changes;

                #region Update attributes

                // compare latest property to the local object
                EgenskabType latestProperty = StubUtil.GetLatestProperty(input.AttributListe.Egenskab);
                if (latestProperty == null || !latestProperty.EnhedNavn.Equals(unit.Name) || (unit.ShortKey != null && !latestProperty.BrugervendtNoegleTekst.Equals(unit.ShortKey)))
                {
                    if (latestProperty == null)
                    {
                        EnsureKeys(unit);
                    }

                    // create a new property
                    EgenskabType newProperty = new EgenskabType();
                    newProperty.Virkning = helper.GetVirkning(unit.Timestamp);
                    newProperty.BrugervendtNoegleTekst = ((unit.ShortKey != null) ? unit.ShortKey : latestProperty.BrugervendtNoegleTekst);
                    newProperty.EnhedNavn = unit.Name;

                    // create a new set of properties
                    input.AttributListe.Egenskab = new EgenskabType[1];
                    input.AttributListe.Egenskab[0] = newProperty;

                    changes = true;
                }
                #endregion

                #region Update address relationships
                // terminate the Virkning on all address relationships that no longer exists locally
                changes = StubUtil.TerminateObjectsInOrgNoLongerPresentLocally(input.RelationListe.Adresser, unit.Addresses, unit.Timestamp, true) || changes;

                // add references to address objects that are new
                List<string> uuidsToAdd = StubUtil.FindAllObjectsInLocalNotInOrg(input.RelationListe.Adresser, unit.Addresses, true);

                if (uuidsToAdd.Count > 0)
                {
                    int size = uuidsToAdd.Count + ((input.RelationListe.Adresser != null) ? input.RelationListe.Adresser.Length : 0);
                    AdresseFlerRelationType[] newAdresser = new AdresseFlerRelationType[size];

                    int i = 0;
                    if (input.RelationListe.Adresser != null)
                    {
                        foreach (var addressInOrg in input.RelationListe.Adresser)
                        {
                            newAdresser[i++] = addressInOrg;
                        }
                    }

                    foreach (string uuidToAdd in uuidsToAdd)
                    {
                        foreach (var addressInLocal in unit.Addresses)
                        {
                            if (addressInLocal.Uuid.Equals(uuidToAdd))
                            {
                                string roleUuid = null;
                                switch (addressInLocal.Type)
                                {
                                    case AddressRelationType.EMAIL:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL;
                                        break;
                                    case AddressRelationType.PHONE:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE;
                                        break;
                                    case AddressRelationType.LOCATION:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOCATION;
                                        break;
                                    case AddressRelationType.LOSSHORTNAME:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSSHORTNAME;
                                        break;
                                    case AddressRelationType.CONTACT_ADDRESS_OPEN_HOURS:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS_OPEN_HOURS;
                                        break;
                                    case AddressRelationType.URL:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_URL;
                                        break;
                                    case AddressRelationType.LANDLINE:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_LANDLINE;
                                        break;
                                    case AddressRelationType.EAN:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_EAN;
                                        break;
                                    case AddressRelationType.EMAIL_REMARKS:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL_REMARKS;
                                        break;
                                    case AddressRelationType.POST_RETURN:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST_RETURN;
                                        break;
                                    case AddressRelationType.CONTACT_ADDRESS:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS;
                                        break;
                                    case AddressRelationType.POST:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST;
                                        break;
                                    case AddressRelationType.PHONE_OPEN_HOURS:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE_OPEN_HOURS;
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

                #region Update organisation relationship
                if (registration.RelationListe.Tilhoerer != null)
                {
                    // make sure that the pointer is set correctly
                    if (!StubUtil.GetMunicipalityOrganisationUUID().Equals(registration.RelationListe.Tilhoerer.ReferenceID.Item))
                    {
                        registration.RelationListe.Tilhoerer.ReferenceID.Item = StubUtil.GetMunicipalityOrganisationUUID();
                        changes = true;
                    }

                    // update the Virkning on the Tilhører relationship if needed (undelete feature)
                    object endTime = registration.RelationListe.Tilhoerer.Virkning.TilTidspunkt.Item;

                    if ((endTime is DateTime) && (DateTime.Compare(DateTime.Now, (DateTime)endTime) >= 0))
                    {
                        log.Debug("Re-establishing relationship with Organisation for OrgUnit " + unit.Uuid);
                        registration.RelationListe.Tilhoerer.Virkning = virkning;
                        changes = true;
                    }
                }
                else
                {
                    // no existing relationship (should actually never happen, but let us just take care of it)
                    helper.AddOrganisationRelation(StubUtil.GetMunicipalityOrganisationUUID(), virkning, registration);
                    changes = true;
                }
                #endregion

                #region Update parent relationship
                if (registration.RelationListe.Overordnet != null)
                {
                    // there is an existing Overordnet relationship, so let us see if there is a change
                    if (unit.ParentOrgUnitUuid != null)
                    {
                        bool expired = false;
                        object endDate = registration.RelationListe.Overordnet.Virkning.TilTidspunkt.Item;

                        if (endDate != null && endDate is DateTime && DateTime.Compare(DateTime.Now, (DateTime) endDate) >= 0)
                        {
                            expired = true;
                        }

                        if (expired || !registration.RelationListe.Overordnet.ReferenceID.Item.Equals(unit.ParentOrgUnitUuid))
                        {
                            // overwrite the existing values (we cannot create multiple references on this, so it is the best we can do with regards to storing full history in the latest registration)
                            registration.RelationListe.Overordnet.ReferenceID = StubUtil.GetReference<UnikIdType>(unit.ParentOrgUnitUuid, ItemChoiceType.UUIDIdentifikator);
                            registration.RelationListe.Overordnet.Virkning = virkning;
                            changes = true;
                        }
                    }
                    else
                    {
                        // attempt to terminate the existing relationship (it might already be terminated)
                        if (StubUtil.TerminateVirkning(registration.RelationListe.Overordnet.Virkning, unit.Timestamp))
                        {
                            changes = true;
                        }
                    }
                }
                else if (unit.ParentOrgUnitUuid != null)
                {
                    // no existing parent, so just create one
                    helper.AddOverordnetEnhed(unit.ParentOrgUnitUuid, virkning, registration);
                    changes = true;
                }
                #endregion

                #region Update opgaver
                if (helper.UpdateOpgaver(unit.Tasks, virkning, registration, unit.Timestamp))
                {
                    changes = true;
                }
                #endregion

                #region Update function references (PayoutUnits and ContactPlaces)
                // terminate the Virkning on all functions (currently there is only one, the payout unit, but this will work for all kinds of functions)
                changes = StubUtil.TerminateObjectsInOrgNoLongerPresentLocally(input.RelationListe.TilknyttedeFunktioner, unit.OrgFunctionUuids, unit.Timestamp, false) || changes;

                // add references to function objects that are new
                List<string> functionUuidsToAdd = StubUtil.FindAllObjectsInLocalNotInOrg(input.RelationListe.TilknyttedeFunktioner, unit.OrgFunctionUuids, false);

                if (functionUuidsToAdd.Count > 0)
                {
                    int size = functionUuidsToAdd.Count + ((input.RelationListe.TilknyttedeFunktioner != null) ? input.RelationListe.TilknyttedeFunktioner.Length : 0);
                    OrganisationFunktionFlerRelationType[] newFunctions = new OrganisationFunktionFlerRelationType[size];

                    int i = 0;
                    if (input.RelationListe.TilknyttedeFunktioner != null)
                    {
                        foreach (var functionsInOrg in input.RelationListe.TilknyttedeFunktioner)
                        {
                            newFunctions[i++] = functionsInOrg;
                        }
                    }

                    foreach (string uuidToAdd in functionUuidsToAdd)
                    {
                        OrganisationFunktionFlerRelationType newFunction = new OrganisationFunktionFlerRelationType();
                        newFunction.ReferenceID = StubUtil.GetReference<UnikIdType>(uuidToAdd, ItemChoiceType.UUIDIdentifikator);
                        newFunction.Virkning = virkning;

                        newFunctions[i++] = newFunction;
                    }

                    input.RelationListe.TilknyttedeFunktioner = newFunctions;
                    changes = true;
                }
                #endregion

                #region Update Type
                if (helper.SetType(unit.OrgUnitType, virkning, registration))
                {
                    changes = true;
                }
                #endregion

                // if no changes are made, we do not call the service
                if (!changes)
                {
                    log.Debug("Ret on OrganisationEnhed with uuid " + unit.Uuid + " cancelled because of no changes");
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
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", OrganisationEnhedStubHelper.SERVICE, response.RetResponse1.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Ret succesful on OrganisationEnhed with uuid " + unit.Uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on OrganisationEnhed", ex);
            }
        }

        public RegistreringType1 GetLatestRegistration(string uuid)
        {
            LaesInputType laesInput = new LaesInputType();
            laesInput.UUIDIdentifikator = uuid;

            laesRequest request = new laesRequest();
            request.LaesRequest1 = new LaesRequestType();
            request.LaesRequest1.LaesInput = laesInput;
            request.LaesRequest1.AuthorityContext = new AuthorityContextType();
            request.LaesRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            OrganisationEnhedPortType channel = StubUtil.CreateChannel<OrganisationEnhedPortType>(OrganisationEnhedStubHelper.SERVICE, "Laes", helper.CreatePort());

            try
            {
                laesResponse response = channel.laes(request);

                int statusCode = Int32.Parse(response.LaesResponse1.LaesOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("Lookup on OrgUnit with uuid '" + uuid + "' failed with statuscode " + statusCode);
                    return null;
                }

                RegistreringType1[] resultSet = response.LaesResponse1.LaesOutput.FiltreretOejebliksbillede.Registrering;
                if (resultSet.Length == 0)
                {
                    log.Warn("OrgUnit with uuid '" + uuid + "' exists, but has no registration");
                    return null;
                }

                RegistreringType1 result = null;
                if (resultSet.Length > 1)
                {
                    log.Warn("OrgUnit with uuid " + uuid + " has more than one registration when reading latest registration, this should never happen");

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
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on OrganisationEnhed", ex);
            }
        }

        public void Deactivate(string uuid, DateTime timestamp)
        {
            log.Debug("Attempting Deactivate on OrganisationEnhed with uuid " + uuid);

            RegistreringType1 registration = GetLatestRegistration(uuid);
            if (registration == null)
            {
                log.Debug("Cannot Deactivate OrganisationEnhed with uuid " + uuid + " because it does not exist in Organisation");
                return;
            }

            OrganisationEnhedPortType channel = StubUtil.CreateChannel<OrganisationEnhedPortType>(OrganisationEnhedStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                RetInputType1 input = new RetInputType1();
                input.UUIDIdentifikator = uuid;
                input.AttributListe = registration.AttributListe;
                input.TilstandListe = registration.TilstandListe;
                input.RelationListe = registration.RelationListe;

                // cut relationship to Parent
                if (input.RelationListe.Overordnet != null)
                {
                    StubUtil.TerminateVirkning(input.RelationListe.Overordnet.Virkning, timestamp);
                }

                // cut relationship to all functions (payout unit references and contact places)
                if (input.RelationListe.TilknyttedeFunktioner != null && input.RelationListe.TilknyttedeFunktioner.Length > 0)
                { 
                    foreach (OrganisationFunktionFlerRelationType funktion in input.RelationListe.TilknyttedeFunktioner)
                    {
                        StubUtil.TerminateVirkning(funktion.Virkning, timestamp);
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
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", OrganisationEnhedStubHelper.SERVICE, response.RetResponse1.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Deactivate successful on OrganisationEnhed with uuid " + uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on OrganisationEnhed", ex);
            }
        }

        public List<string> Soeg(string antal = null, string offset = null)
        {
            OrganisationEnhedPortType channel = StubUtil.CreateChannel<OrganisationEnhedPortType>(OrganisationEnhedStubHelper.SERVICE, "Soeg", helper.CreatePort());

            SoegInputType1 soegInput = new SoegInputType1();
            soegInput.AttributListe = new AttributListeType();
            soegInput.RelationListe = new RelationListeType();
            soegInput.TilstandListe = new TilstandListeType();

            if (antal != null)
            {
                soegInput.MaksimalAntalKvantitet = antal;
            }
            if (offset != null)
            {
                soegInput.FoersteResultatReference = offset;
            }

            // only search for Active units
            soegInput.TilstandListe.Gyldighed = new GyldighedType[1];
            soegInput.TilstandListe.Gyldighed[0] = new GyldighedType();
            soegInput.TilstandListe.Gyldighed[0].GyldighedStatusKode = GyldighedStatusKodeType.Aktiv;

            // TODO: these three lines should be removeable once KMD fixes their end
            soegInput.TilstandListe.Gyldighed[0].Virkning = new VirkningType();
            soegInput.TilstandListe.Gyldighed[0].Virkning.FraTidspunkt = new TidspunktType();
            soegInput.TilstandListe.Gyldighed[0].Virkning.FraTidspunkt.Item = DateTime.Now;

            // only return objects that have a Tilhører relationship top-level Organisation
            UnikIdType orgReference = StubUtil.GetReference<UnikIdType>(registry.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()], ItemChoiceType.UUIDIdentifikator);
            OrganisationRelationType organisationRelationType = new OrganisationRelationType();
            organisationRelationType.ReferenceID = orgReference;
            soegInput.RelationListe.Tilhoerer = organisationRelationType;

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
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Soeg", OrganisationEnhedStubHelper.SERVICE, response.SoegResponse1.SoegOutput.StandardRetur.FejlbeskedTekst);
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
                throw new ServiceNotFoundException("Failed to establish connection to the Soeg service on OrganisationEnhed", ex);
            }
        }

        private void EnsureKeys(OrgUnitData unit)
        {
            unit.ShortKey = (unit.ShortKey != null) ? unit.ShortKey : IdUtil.GenerateShortKey();
        }
    }
}
