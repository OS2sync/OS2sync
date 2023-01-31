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
        private OrganisationRegistryProperties registry = OrganisationRegistryProperties.GetInstance();

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
            ImportInputType inportInput = new ImportInputType();
            inportInput.Adresse = addresseType;

            // construct request
            importerRequest request = new importerRequest();
            request.ImporterRequest1 = new ImporterRequestType();
            request.ImporterRequest1.ImportInput = inportInput;
            request.ImporterRequest1.AuthorityContext = new AuthorityContextType();
            request.ImporterRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send request
            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Importer", helper.CreatePort());

            try
            {
                importerResponse response = channel.importer(request);
                int statusCode = Int32.Parse(response.ImporterResponse1.ImportOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    if (statusCode == 49) // object already exists is the most likely scenario here
                    {
                        // TODO: a better approach would be to try the read-then-update-if-exists-else-create approach we use elsewhere
                        log.Info("Skipping import on Address " + address.Uuid + " as Organisation returned status 49. The most likely cause is that the object already exists");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Import", AdresseStubHelper.SERVICE, response.ImporterResponse1.ImportOutput.StandardRetur.FejlbeskedTekst);
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

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Ret", helper.CreatePort());

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
                        log.Warn("Ret failed on Address " + uuid + " as Organisation returned status 49. The most likely cause is that the object has been Passiveret");
                        return;
                    }

                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "Ret", AdresseStubHelper.SERVICE, response.RetResponse1.RetOutput.StandardRetur.FejlbeskedTekst);
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

        public RegistreringType1 GetLatestRegistration(string uuid)
        {
            LaesInputType laesInput = new LaesInputType();
            laesInput.UUIDIdentifikator = uuid;

            laesRequest request = new laesRequest();
            request.LaesRequest1 = new LaesRequestType();
            request.LaesRequest1.LaesInput = laesInput;
            request.LaesRequest1.AuthorityContext = new AuthorityContextType();
            request.LaesRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Laes", helper.CreatePort());

            try
            {
                laesResponse response = channel.laes(request);

                int statusCode = Int32.Parse(response.LaesResponse1.LaesOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("Lookup on Adresse with uuid '" + uuid + "' failed with statuscode " + statusCode);
                    return null;
                }

                RegistreringType1[] resultSet = response.LaesResponse1.LaesOutput.FiltreretOejebliksbillede.Registrering;
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
            request.ListRequest1 = new ListRequestType();
            request.ListRequest1.ListInput = listInput;
            request.ListRequest1.AuthorityContext = new AuthorityContextType();
            request.ListRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "List", helper.CreatePort());

            try
            {
                listResponse response = channel.list(request);

                int statusCode = Int32.Parse(response.ListResponse1.ListOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    // note that statusCode 44 means that the object does not exists, so that is a valid response
                    log.Debug("List on Adresse failed with statuscode " + statusCode);
                    return result;
                }

                if (response.ListResponse1.ListOutput.FiltreretOejebliksbillede == null || response.ListResponse1.ListOutput.FiltreretOejebliksbillede.Length == 0)
                {
                    log.Debug("List on Adresse has 0 hits");
                    return result;
                }

                foreach (var adresse in response.ListResponse1.ListOutput.FiltreretOejebliksbillede)
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