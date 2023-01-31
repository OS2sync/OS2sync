using IntegrationLayer.Bruger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class BrugerStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private BrugerStubHelper helper = new BrugerStubHelper();
        private OrganisationRegistryProperties registry = OrganisationRegistryProperties.GetInstance();

        public void Importer(UserData user)
        {
            // create ShortKey if not supplied
            EnsureKeys(user);

            log.Debug("Attempting Import on Bruger with uuid " + user.Uuid);

            // create timestamp object to be used on all registrations, properties and relations
            VirkningType virkning = helper.GetVirkning(user.Timestamp);

            // setup registration
            RegistreringType1 registration = helper.CreateRegistration(user.Timestamp, LivscyklusKodeType.Importeret);

            // add properties
            helper.AddProperties(user.ShortKey, user.UserId, virkning, registration);

            // setup relations
            helper.AddAddressReferences(user.Addresses, virkning, registration);
            helper.AddPersonRelationship(user.PersonUuid, virkning, registration);
            helper.AddOrganisationRelation(StubUtil.GetMunicipalityOrganisationUUID(), virkning, registration);

            // set Tilstand to Active
            helper.SetTilstandToActive(virkning, registration, user.Timestamp);

            // wire everything together
            BrugerType brugerType = helper.GetBrugerType(user.Uuid, registration);
            ImportInputType importInput = new ImportInputType();
            importInput.Bruger = brugerType;

            // construct request
            importerRequest request = new importerRequest();
            request.ImporterRequest1 = new ImporterRequestType();
            request.ImporterRequest1.ImportInput = importInput;
            request.ImporterRequest1.AuthorityContext = new AuthorityContextType();
            request.ImporterRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send request
            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Importer", helper.CreatePort());

            try
            {
                importerResponse response = channel.importer(request);
                int statusCode = Int32.Parse(response.ImporterResponse1.ImportOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49)
                    {
                        log.Warn("Importer failed on Bruger " + user.Uuid + " as Organisation returned status 49. The most likely cause is that the object has been imported");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Import", BrugerStubHelper.SERVICE, response.ImporterResponse1.ImportOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);

                    throw new SoapServiceException(message);
                }

                log.Debug("Import on Bruger with uuid " + user.Uuid + " succeeded");
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on Bruger", ex);
            }
        }

        // Deactivates the user by setting the Gyldighed attribute to Inactive
        public void Deactivate(string uuid, DateTime timestamp)
        {
            log.Debug("Attempting Deactivate on Bruger with uuid " + uuid);

            RegistreringType1 registration = GetLatestRegistration(uuid);
            if (registration == null)
            {
                log.Debug("Cannot Deactivate Bruger with uuid " + uuid + " because it does not exist in Organisation");
                return;
            }

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                RetInputType1 input = new RetInputType1();
                input.UUIDIdentifikator = uuid;
                input.AttributListe = registration.AttributListe;
                input.TilstandListe = registration.TilstandListe;
                input.RelationListe = registration.RelationListe;

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
                    if (statusCode == 49)
                    {
                        log.Warn("Deactive failed on Bruger " + uuid + " as Organisation returned status 49. The most likely cause is that the object has been Passiveret");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", BrugerStubHelper.SERVICE, response.RetResponse1.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Deactivate on Bruger with uuid " +  uuid + " succeeded");
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on Bruger", ex);
            }
        }

        public void Ret(UserData user)
        {
            log.Debug("Attempting Ret on Bruger with uuid " + user.Uuid);

            RegistreringType1 registration = GetLatestRegistration(user.Uuid);
            if (registration == null)
            {
                log.Debug("Cannot call Ret on Bruger with uuid " + user.Uuid + " because it does not exist in Organisation");
                return;
            }

            VirkningType virkning = helper.GetVirkning(user.Timestamp);

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                bool changes = false;

                RetInputType1 input = new RetInputType1();
                input.UUIDIdentifikator = user.Uuid;
                input.AttributListe = registration.AttributListe;
                input.TilstandListe = registration.TilstandListe;
                input.RelationListe = registration.RelationListe;

                // set Tilstand to Active (idempotent change - but it ensures that a previously deactived object is re-activated)
                changes = helper.SetTilstandToActive(virkning, registration, user.Timestamp) | changes;

                #region Update attributes

                // compare latest property to the local object
                EgenskabType latestProperty = StubUtil.GetLatestProperty(input.AttributListe.Egenskab);
                if (latestProperty == null ||
                    latestProperty.BrugerNavn == null ||
                    latestProperty.BrugervendtNoegleTekst == null ||
                    !string.Equals(latestProperty.BrugerNavn, user.UserId) ||
                   (!string.IsNullOrEmpty(user.ShortKey) && !string.Equals(latestProperty.BrugervendtNoegleTekst, user.ShortKey)))
                {
                    // end the validity of open-ended property
                    if (latestProperty == null || latestProperty.BrugervendtNoegleTekst == null)
                    {
                        // create ShortKey if not supplied
                        EnsureKeys(user);
                    }

                    // create a new property
                    EgenskabType newProperty = new EgenskabType();
                    newProperty.Virkning = helper.GetVirkning(user.Timestamp);

                    newProperty.BrugervendtNoegleTekst = !string.IsNullOrEmpty(user.ShortKey)
                        ? user.ShortKey
                        : (!string.IsNullOrEmpty(latestProperty.BrugervendtNoegleTekst)
                            ? latestProperty.BrugervendtNoegleTekst
                            : IdUtil.GenerateShortKey());

                    newProperty.BrugerNavn = user.UserId;

                    // create a new set of properties
                    input.AttributListe.Egenskab = new EgenskabType[1];
                    input.AttributListe.Egenskab[0] = newProperty;

                    changes = true;
                }
                #endregion

                #region Update address relationships
                // terminate the Virkning on all address relationships that no longer exists locally
                changes = StubUtil.TerminateObjectsInOrgNoLongerPresentLocally(input.RelationListe.Adresser, user.Addresses, user.Timestamp, true) || changes;

                // add references to address objects that are new
                List<string> uuidsToAdd = StubUtil.FindAllObjectsInLocalNotInOrg(input.RelationListe.Adresser, user.Addresses, true);

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
                        foreach (var addressInLocal in user.Addresses)
                        {
                            if (addressInLocal.Uuid.Equals(uuidToAdd))
                            {
                                string roleUuid = null;
                                switch (addressInLocal.Type)
                                {
                                    case AddressRelationType.EMAIL:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_USER_EMAIL;
                                        break;
                                    case AddressRelationType.PHONE:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_USER_PHONE;
                                        break;
                                    case AddressRelationType.LOCATION:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_USER_LOCATION;
                                        break;
                                    case AddressRelationType.LANDLINE:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_USER_LANDLINE;
                                        break;
                                    case AddressRelationType.RACFID:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_USER_RACFID;
                                        break;
                                    case AddressRelationType.FMKID:
                                        roleUuid = UUIDConstants.ADDRESS_ROLE_USER_FMKID;
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

                    if (endTime is DateTime && (DateTime.Compare(DateTime.Now, (DateTime)endTime) >= 0))
                    {
                        log.Debug("Re-establishing relationship with Organisation for Bruger " + user.Uuid);
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

                #region Update person relationship
                PersonFlerRelationType existingPerson = BrugerStubHelper.GetLatestPersonFlerRelationType(registration.RelationListe.TilknyttedePersoner);
                if (existingPerson != null)
                {
                    // It really shouldn't happen that often that the Person reference changes on a User, but we support it nonetheless
                    if (!existingPerson.ReferenceID.Item.Equals(user.PersonUuid))
                    {
                        // terminiate existing relationship, and add a new one
                        StubUtil.TerminateVirkning(existingPerson.Virkning, user.Timestamp);

                        // create a new person relation
                        PersonFlerRelationType newPerson = new PersonFlerRelationType();
                        newPerson.Virkning = helper.GetVirkning(user.Timestamp);
                        newPerson.ReferenceID = StubUtil.GetReference<UnikIdType>(user.PersonUuid, ItemChoiceType.UUIDIdentifikator);

                        // create a new set of person references, containing the new user
                        PersonFlerRelationType[] oldPersons = registration.RelationListe.TilknyttedePersoner;
                        input.RelationListe.TilknyttedePersoner = new PersonFlerRelationType[oldPersons.Length + 1];
                        for (int i = 0; i < oldPersons.Length; i++)
                        {
                            input.RelationListe.TilknyttedePersoner[i] = oldPersons[i];
                        }
                        input.RelationListe.TilknyttedePersoner[oldPersons.Length] = newPerson;

                        changes = true;

                    }
                }
                else
                {
                    // This really shouldn't have happened, as it means we have an existing User without a Person attached - but
                    // we will just fix it, and create the reference on this user

                    log.Warn("Ret on Bruge with uuid " + user.Uuid + " encountered a registration with NO relationship to a Person - fixing it!");
                    helper.AddPersonRelationship(user.PersonUuid, virkning, registration);
                    changes = true;
                }
                #endregion

                // if no changes are made, we do not call the service
                if (!changes)
                {
                    log.Debug("Ret on Bruger with uuid " + user.Uuid + " cancelled because of no changes");
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
                        log.Warn("Ret failed on Bruger " + user.Uuid + " as Organisation returned status 49. The most likely cause is that the object has been Passiveret");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", BrugerStubHelper.SERVICE, response.RetResponse1.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Ret succesful on Bruger with uuid " + user.Uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                // temporary fix until we figure out why this happens
                if (ex.Message.Contains("Fault occurred while processing")) {
                    throw;
                }
                
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on Bruger", ex);
            }
        }

        public Dictionary<string, RegistreringType1> GetLatestRegistrations(List<string> userUuids)
        {
            var result = new Dictionary<string, RegistreringType1>();

            ListInputType listInput = new ListInputType();
            listInput.UUIDIdentifikator = userUuids.ToArray();

            listRequest request = new listRequest();
            request.ListRequest1 = new ListRequestType();
            request.ListRequest1.ListInput = listInput;
            request.ListRequest1.AuthorityContext = new AuthorityContextType();
            request.ListRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "List", helper.CreatePort());

            try
            {
                listResponse response = channel.list(request);

                int statusCode = Int32.Parse(response.ListResponse1.ListOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("List on Bruger failed with statuscode " + statusCode);
                    return result;
                }

                if (response.ListResponse1.ListOutput.FiltreretOejebliksbillede == null || response.ListResponse1.ListOutput.FiltreretOejebliksbillede.Length == 0)
                {
                    log.Debug("List on Bruger has 0 hits");
                    return result;
                }

                foreach (var user in response.ListResponse1.ListOutput.FiltreretOejebliksbillede)
                {
                    RegistreringType1[] resultSet = user.Registrering;
                    if (resultSet.Length == 0)
                    {
                        log.Warn("Bruger with uuid '" + user.ObjektType.UUIDIdentifikator + "' exists, but has no registration");
                        continue;
                    }

                    RegistreringType1 reg = null;
                    if (resultSet.Length > 1)
                    {
                        log.Warn("Bruger with uuid " + user.ObjektType.UUIDIdentifikator + " has more than one registration when reading latest registration, this should never happen");

                        DateTime winner = DateTime.MinValue;
                        foreach (RegistreringType1 res in resultSet)
                        {
                            // first time through will always result in a True evaluation here
                            if (DateTime.Compare(winner, res.Tidspunkt) < 0)
                            {
                                reg = res;
                                winner = res.Tidspunkt;
                            }
                        }
                    }
                    else
                    {
                        reg = resultSet[0];
                    }

                    // we cannot perform any kind of updates on Slettet/Passiveret, så it makes sense to filter them out on lookup,
                    // so the rest of the code will default to Import op top of this
                    if (reg.LivscyklusKode.Equals(LivscyklusKodeType.Slettet) || reg.LivscyklusKode.Equals(LivscyklusKodeType.Passiveret))
                    {
                        continue;
                    }

                    result.Add(user.ObjektType.UUIDIdentifikator, reg);
                }

                return result;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the List service on Bruger", ex);
            }
        }

        public List<string> Soeg()
        {
            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Soeg", helper.CreatePort());

            SoegInputType1 soegInput = new SoegInputType1();
            soegInput.AttributListe = new AttributListeType();
            soegInput.RelationListe = new RelationListeType();
            soegInput.TilstandListe = new TilstandListeType();

            // only search for Active users
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

            /*
            // TODO: see if this gives us what we want
            soegInput.SoegRegistrering = new SoegRegistreringType();
            soegInput.SoegRegistrering.FraTidspunkt = new TidspunktType();
            soegInput.SoegRegistrering.FraTidspunkt.Item = new DateTime(2018, 8, 29, 14, 10, 00, DateTimeKind.Local);
            */

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
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Soeg", BrugerStubHelper.SERVICE, response.SoegResponse1.SoegOutput.StandardRetur.FejlbeskedTekst);
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
                throw new ServiceNotFoundException("Failed to establish connection to the Soeg service on Bruger", ex);
            }
        }

        public bool IsAlive()
        {
            LaesInputType laesInput = new LaesInputType();
            laesInput.UUIDIdentifikator = Guid.NewGuid().ToString().ToLower();

            laesRequest request = new laesRequest();
            request.LaesRequest1 = new LaesRequestType();
            request.LaesRequest1.LaesInput = laesInput;
            request.LaesRequest1.AuthorityContext = new AuthorityContextType();
            request.LaesRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Laes", helper.CreatePort());

            try
            {
                laesResponse response = channel.laes(request);

                int statusCode = Int32.Parse(response.LaesResponse1.LaesOutput.StandardRetur.StatusKode);
                if (statusCode == 20 || statusCode == 44)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                ; // ignore
            }

            return false;
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

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Laes", helper.CreatePort());

            try
            {
                laesResponse response = channel.laes(request);

                int statusCode = Int32.Parse(response.LaesResponse1.LaesOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("Lookup on Bruger with uuid '" + uuid + "' failed with statuscode " + statusCode);
                    return null;
                }

                RegistreringType1[] resultSet = response.LaesResponse1.LaesOutput.FiltreretOejebliksbillede.Registrering;
                if (resultSet.Length == 0)
                {
                    log.Warn("Bruger with uuid '" + uuid + "' exists, but has no registration");
                    return null;
                }

                RegistreringType1 result = null;
                if (resultSet.Length > 1)
                {
                    log.Warn("Bruger with uuid " + uuid + " has more than one registration when reading latest registration, this should never happen");

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
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on Bruger", ex);
            }
        }

        private void EnsureKeys(UserData bruger)
        {
            bruger.ShortKey = (bruger.ShortKey != null) ? bruger.ShortKey : IdUtil.GenerateShortKey();
        }
    }
}
