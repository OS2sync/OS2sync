using IntegrationLayer.Bruger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Organisation.IntegrationLayer
{
    internal class BrugerStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private BrugerStubHelper helper = new BrugerStubHelper();

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
            request.ImportInput = importInput;

            // send request
            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Importer");

            try
            {
                importerResponse response = channel.importerAsync(request).Result;
                int statusCode = Int32.Parse(response.ImportOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49)
                    {
                        log.Warn("Importer failed on Bruger " + user.Uuid + " as Organisation returned status 49. The most likely cause is that the object has been imported");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Import", BrugerStubHelper.SERVICE, response.ImportOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);

                    throw new SoapServiceException(message);
                }

                log.Debug("Import on Bruger with uuid " + user.Uuid + " succeeded");
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException || ex is AggregateException)
            {
                throw StubUtil.CheckForTemporaryError(ex, "Importer", "Bruger");
            }
        }

        public void Passiver(string uuid)
        {
            log.Debug("Attempting Passiver on Bruger with uuid " + uuid);

            // construct request
            UuidNoteInputType input = new UuidNoteInputType();
            input.UUIDIdentifikator = uuid;

            passiverRequest request = new passiverRequest();
            request.PassiverInput = input;

            // send request
            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Passiver");

            try
            {
                var result = channel.passiverAsync(request).Result;
                int statusCode = Int32.Parse(result.PassiverOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Passiver", BrugerStubHelper.SERVICE, result.PassiverOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Passiver successful on Bruger with uuid " + uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException || ex is AggregateException)
            {
                throw StubUtil.CheckForTemporaryError(ex, "Passiver", "Bruger");
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

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Ret");

            try
            {
                RetInputType1 input = new RetInputType1();
                input.UUIDIdentifikator = uuid;
                input.AttributListe = registration.AttributListe;
                input.TilstandListe = registration.TilstandListe;
                input.RelationListe = registration.RelationListe;

                VirkningType virkning = helper.GetVirkning(timestamp);
                helper.SetTilstandToInactive(virkning, registration, timestamp);

                if (registration.RelationListe?.TilknyttedePersoner != null)
                {
                    foreach (var personRelation in registration.RelationListe.TilknyttedePersoner)
                    {
                        StubUtil.TerminateVirkning(personRelation.Virkning, timestamp);
                    }
                }

                if (registration.RelationListe?.Tilhoerer != null)
                {
                    StubUtil.TerminateVirkning(registration.RelationListe.Tilhoerer.Virkning, timestamp);
                }

                // we cannot fix the actual data issue in FK Organisation, but we can remove them from
                // the payload, so the validator does not reject our update *sigh*
                removeDuplicateAddresses(registration);

                retRequest request = new retRequest();
                request.RetInput = input;

                retResponse response = channel.retAsync(request).Result;

                int statusCode = Int32.Parse(response.RetOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49)
                    {
                        log.Warn("Deactive failed on Bruger " + uuid + " as Organisation returned status 49. The most likely cause is that the object has been Passiveret");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", BrugerStubHelper.SERVICE, response.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Deactivate on Bruger with uuid " +  uuid + " succeeded");
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException || ex is AggregateException)
            {
                throw StubUtil.CheckForTemporaryError(ex, "Deactivate", "Bruger");
            }
        }

        private void removeDuplicateAddresses(RegistreringType1 registration)
        {
            if (registration.RelationListe?.Adresser != null)
            {
                var toRemove = new List<int>();
                var seen = new List<string>();

                // identify elements to remove (duplicates)
                for (int i = registration.RelationListe.Adresser.Length - 1; i >= 0; i--)
                {
                    if (seen.Contains(registration.RelationListe.Adresser[i].ReferenceID.Item))
                    {
                        toRemove.Add(i);
                        continue;
                    }

                    seen.Add(registration.RelationListe.Adresser[i].ReferenceID.Item);
                }

                // copy those we want to keep into new array
                if (toRemove.Count > 0)
                {
                    AdresseFlerRelationType[] addresses = new AdresseFlerRelationType[registration.RelationListe.Adresser.Length - toRemove.Count];

                    int j = addresses.Length - 1;
                    for (int i = registration.RelationListe.Adresser.Length - 1; i >= 0; i--)
                    {
                        if (toRemove.Contains(i))
                        {
                            continue;
                        }

                        addresses[j--] = registration.RelationListe.Adresser[i];
                    }

                    registration.RelationListe.Adresser = addresses;
                }
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

            // identify if duplicate address references are present, and perform passiver/import in that case
            if (OrganisationRegistryProperties.AppSettings.PassiverAndReImportOnErrors)
            {
                var addresses = registration?.RelationListe?.Adresser;
                if (addresses != null)
                {
                    List<string> addressReferenceUuids = new List<string>();

                    foreach (var address in addresses)
                    {
                        string uuidReference = address.ReferenceID.Item;
                        if (addressReferenceUuids.Contains(uuidReference))
                        {
                            log.Info("Detected address duplicates on " + user.Uuid + " performing Passiver and Import");
                            Passiver(user.Uuid);
                            Importer(user);

                            return;
                        }

                        addressReferenceUuids.Add(uuidReference);
                    }
                }
            }

            VirkningType virkning = helper.GetVirkning(user.Timestamp);

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Ret");

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

                                AdresseFlerRelationType newAddress = helper.CreateAddressReference(uuidToAdd, roleUuid, virkning);
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

                // we cannot fix the actual data issue in FK Organisation, but we can remove them from
                // the payload, so the validator does not reject our update *sigh*
                removeDuplicateAddresses(registration);

                // send Ret request
                retRequest request = new retRequest();
                request.RetInput = input;

                retResponse response = channel.retAsync(request).Result;

                int statusCode = Int32.Parse(response.RetOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49)
                    {
                        log.Warn("Ret failed on Bruger " + user.Uuid + " as Organisation returned status 49. The most likely cause is that the object has been Passiveret");
                        return;
                    }
                    else if (statusCode == 40 && OrganisationRegistryProperties.AppSettings.PassiverAndReImportOnErrors)
                    {
                        log.Warn("Ret failed on Bruger " + user.Uuid + " with errorCode 40 - attempting Passiver followed by Importer");

                        Passiver(user.Uuid);
                        Importer(user);

                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", BrugerStubHelper.SERVICE, response.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Ret succesful on Bruger with uuid " + user.Uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException || ex is AggregateException)
            {
                throw StubUtil.CheckForTemporaryError(ex, "Ret", "Bruger");
            }
        }

        public Dictionary<string, RegistreringType1> GetLatestRegistrations(List<string> userUuids)
        {
            var result = new Dictionary<string, RegistreringType1>();

            ListInputType listInput = new ListInputType();
            listInput.UUIDIdentifikator = userUuids.ToArray();

            listRequest request = new listRequest();
            request.ListInput = listInput;

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "List");

            try
            {
                listResponse response = channel.listAsync(request).Result;

                int statusCode = Int32.Parse(response.ListOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("List on Bruger failed with statuscode " + statusCode);
                    return result;
                }

                if (response.ListOutput.FiltreretOejebliksbillede == null || response.ListOutput.FiltreretOejebliksbillede.Length == 0)
                {
                    log.Debug("List on Bruger has 0 hits");
                    return result;
                }

                foreach (var user in response.ListOutput.FiltreretOejebliksbillede)
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
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException || ex is AggregateException)
            {
                throw StubUtil.CheckForTemporaryError(ex, "List", "Bruger");
            }
        }

        public List<string> Soeg(int offset, int amount)
        {
            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Soeg");

            SoegInputType1 soegInput = new SoegInputType1();
            soegInput.AttributListe = new AttributListeType();
            soegInput.RelationListe = new RelationListeType();
            soegInput.TilstandListe = new TilstandListeType();

            // only search for Active users
            soegInput.TilstandListe.Gyldighed = new GyldighedType[1];
            soegInput.TilstandListe.Gyldighed[0] = new GyldighedType();
            soegInput.TilstandListe.Gyldighed[0].GyldighedStatusKode = GyldighedStatusKodeType.Aktiv;

            // only return objects that have a Tilhører relationship top-level Organisation
            UnikIdType orgReference = StubUtil.GetReference<UnikIdType>(OrganisationRegistryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()], ItemChoiceType.UUIDIdentifikator);
            OrganisationFlerRelationType organisationRelationType = new OrganisationFlerRelationType();
            organisationRelationType.ReferenceID = orgReference;
            soegInput.RelationListe.Tilhoerer = organisationRelationType;

            // search
            soegRequest request = new soegRequest();
            request.SoegInput = soegInput;
            request.SoegInput.MaksimalAntalKvantitet = amount.ToString();
            request.SoegInput.FoersteResultatReference = offset.ToString();

            try
            {
                soegResponse response = channel.soegAsync(request).Result;
                int statusCode = Int32.Parse(response.SoegOutput.StandardRetur.StatusKode);
                if (statusCode != 20 && statusCode != 44) // 44 is empty search result
                {
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Soeg", BrugerStubHelper.SERVICE, response.SoegOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                List<string> functions = new List<string>();
                if (statusCode == 20)
                {
                    foreach (string id in response.SoegOutput.IdListe)
                    {
                        functions.Add(id);
                    }
                }

                return functions;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException || ex is AggregateException)
            {
                throw StubUtil.CheckForTemporaryError(ex, "Soeg", "Bruger");
            }
        }

        public bool IsAlive()
        {
            LaesInputType laesInput = new LaesInputType();
            laesInput.UUIDIdentifikator = Guid.NewGuid().ToString().ToLower();

            laesRequest request = new laesRequest();
            request.LaesInput = laesInput;

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Laes");

            try
            {
                laesResponse response = channel.laesAsync(request).Result;

                int statusCode = Int32.Parse(response.LaesOutput.StandardRetur.StatusKode);
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
            request.LaesInput = laesInput;

            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Laes");

            try
            {
                laesResponse response = channel.laesAsync(request).Result;

                int statusCode = Int32.Parse(response.LaesOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("Lookup on Bruger with uuid '" + uuid + "' failed with statuscode " + statusCode);
                    return null;
                }

                RegistreringType1[] resultSet = response.LaesOutput.FiltreretOejebliksbillede.Registrering;
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
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException || ex is AggregateException)
            {
                throw StubUtil.CheckForTemporaryError(ex, "Laes", "Bruger");
            }
        }

        private void EnsureKeys(UserData bruger)
        {
            bruger.ShortKey = (bruger.ShortKey != null) ? bruger.ShortKey : IdUtil.GenerateShortKey();
        }
    }
}
