using IntegrationLayer.Adresse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class AdresseStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private AdresseStubHelper helper = new AdresseStubHelper();

        public void Importer(AddressData address)
        {
            // create ShortKey and Uuid if not supplied
            EnsureKeys(address);

            log.Debug("Attempting Import on Address with uuid " + address.Uuid);

            // create timestamp object to be used on all registrations, properties and relations
            VirkningType virkning = helper.GetVirkning(address.Timestamp);

            // setup registration
            RegistreringType1 registration = helper.CreateRegistration(address.Timestamp, LivscyklusKodeType.Importeret);

            // add properties
            helper.AddProperties(address.AddressText, address.ShortKey, virkning, registration);

            // wire everything together
            AdresseType addresseType = helper.GetAdresseType(address.Uuid, registration);
            ImportInputType importInput = new ImportInputType();
            importInput.Adresse = addresseType;

            // construct request
            importerRequest request = new importerRequest();
            request.ImportInput = importInput;

            // send request
            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Importer");
            
            try
            {
                importerResponse response = channel.importerAsync(request).Result;
                int statusCode = Int32.Parse(response.ImportOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49) // object already exists is the most likely scenario here
                    {
                        // TODO: a better approach would be to try the read-then-update-if-exists-else-create approach we use elsewhere
                        log.Info("Skipping import on Address " + address.Uuid + " as Organisation returned status 49. The most likely cause is that the object already exists");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Import", AdresseStubHelper.SERVICE, response.ImportOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Import successful on Address with uuid " + address.Uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on Adresse", ex);
            }
        }

        // this method only performs a call to Ret on Address if there are actual changes
        public void Ret(string uuid, string newValue, DateTime timestamp, RegistreringType1 registration)
        {
            log.Debug("Attempting Ret on Address with uuid " + uuid);

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Ret");

            try
            {
                RetInputType1 input = new RetInputType1();
                input.UUIDIdentifikator = uuid;
                input.AttributListe = registration.AttributListe;
                input.TilstandListe = registration.TilstandListe;
                input.RelationListe = registration.RelationListe;

                // compare latest property to the local object
                EgenskabType latestProperty = StubUtil.GetLatestProperty(input.AttributListe);
                if (latestProperty == null || latestProperty.BrugervendtNoegleTekst == null || !latestProperty.AdresseTekst.Equals(newValue))
                {
                    // create a new property
                    EgenskabType newProperty = new EgenskabType();
                    newProperty.Virkning = helper.GetVirkning(timestamp);
                    newProperty.BrugervendtNoegleTekst = !string.IsNullOrEmpty(latestProperty?.BrugervendtNoegleTekst) ? latestProperty.BrugervendtNoegleTekst : IdUtil.GenerateShortKey();
                    newProperty.AdresseTekst = newValue;

                    // create a new set of properties
                    input.AttributListe = new EgenskabType[1];
                    input.AttributListe[0] = newProperty;
                }
                else
                {
                    log.Debug("No changes on Address, so returning without calling Organisation");

                    // if there are no changes to the attributes, we do not call the Organisation service
                    return;
                }

                // send Ret request
                retRequest request = new retRequest();
                request.RetInput = input;

                retResponse response = channel.retAsync(request).Result;

                int statusCode = Int32.Parse(response.RetOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49)
                    {
                        log.Warn("Ret failed on Address " + uuid + " as Organisation returned status 49. The most likely cause is that the object has been Passiveret");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", AdresseStubHelper.SERVICE, response.RetOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                log.Debug("Ret successful on Address with uuid " + uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on Adresse", ex);
            }
        }

        public void Delete(string uuid)
        {
            log.Debug("Attempting Delete on Address with uuid " + uuid);

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Delete");

            try
            {
                UuidNoteInputType input = new UuidNoteInputType();
                input.UUIDIdentifikator = uuid;

                sletRequest request = new sletRequest();
                request.SletInput = input;

                sletResponse response = channel.sletAsync(request).Result;

                int statusCode = Int32.Parse(response.SletOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49 || statusCode == 44)
                    {
                        log.Warn("Could not delete address with uuid " + uuid + ", because it likely does not exist or is already deleted: statusCode = " + statusCode);
                    }
                    else
                    {
                        string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Slet", AdresseStubHelper.SERVICE, response.SletOutput.StandardRetur.FejlbeskedTekst);
                        log.Error(message);
                        throw new SoapServiceException(message);
                    }
                }

                log.Debug("Slet successful on Address with uuid " + uuid);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Slet service on Adresse", ex);
            }
        }

        public RegistreringType1 GetLatestRegistration(string uuid)
        {
            LaesInputType laesInput = new LaesInputType();
            laesInput.UUIDIdentifikator = uuid;

            laesRequest request = new laesRequest();
            request.LaesInput = laesInput;

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Laes");

            try
            {
                laesResponse response = channel.laesAsync(request).Result;

                int statusCode = Int32.Parse(response.LaesOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("Lookup on Adresse with uuid '" + uuid + "' failed with statuscode " + statusCode);
                    return null;
                }

                RegistreringType1[] resultSet = response.LaesOutput.FiltreretOejebliksbillede.Registrering;
                if (resultSet.Length == 0)
                {
                    log.Warn("Adresse with uuid '" + uuid + "' exists, but has no registration");
                    return null;
                }

                RegistreringType1 result = null;
                if (resultSet.Length > 1)
                {
                    log.Warn("Adresse with uuid " + uuid + " has more than one registration when reading latest registration, this should never happen");

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
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on Adresse", ex);
            }
        }

        public Dictionary<string, RegistreringType1> GetLatestRegistrations(List<string> uuids)
        {
            var result = new Dictionary<string, RegistreringType1>();

            ListInputType listInput = new ListInputType();
            listInput.UUIDIdentifikator = uuids.ToArray();

            listRequest request = new listRequest();
            request.ListInput = listInput;

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "List");

            try
            {
                listResponse response = channel.listAsync(request).Result;

                int statusCode = Int32.Parse(response.ListOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("List on Adresse failed with statuscode " + statusCode);
                    return result;
                }

                if (response.ListOutput.FiltreretOejebliksbillede == null || response.ListOutput.FiltreretOejebliksbillede.Length == 0)
                {
                    log.Debug("List on Adresse has 0 hits");
                    return result;
                }

                foreach (var adresse in response.ListOutput.FiltreretOejebliksbillede)
                {
                    RegistreringType1[] resultSet = adresse.Registrering;
                    if (resultSet.Length == 0)
                    {
                        log.Warn("Adresse with uuid '" + adresse.ObjektType.UUIDIdentifikator + "' exists, but has no registration");
                        continue;
                    }

                    RegistreringType1 reg = null;
                    if (resultSet.Length > 1)
                    {
                        log.Warn("Adresse with uuid " + adresse.ObjektType.UUIDIdentifikator + " has more than one registration when reading latest registration, this should never happen");

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

                    result.Add(adresse.ObjektType.UUIDIdentifikator, reg);
                }

                return result;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the List service on Adresse", ex);
            }
        }

        private void EnsureKeys(AddressData address)
        {
            address.Uuid = (address.Uuid != null) ? address.Uuid : IdUtil.GenerateUuid();
            address.ShortKey = (address.ShortKey != null) ? address.ShortKey : IdUtil.GenerateShortKey();
        }
    }
}