using IntegrationLayer.OrganisationSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class OrganisationSystemStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrganisationSystemStubHelper helper = new OrganisationSystemStubHelper();
        private OrganisationRegistryProperties registry = OrganisationRegistryProperties.GetInstance();

        public List<OrgUnitRegWrapper> Read(string antal, string offset, out Boolean moreData)
        {
            moreData = false; // initialize to no-more-data

            FremsoegObjekthierarkiInputType input = new FremsoegObjekthierarkiInputType();
            input.MaksimalAntalKvantitet = antal;
            input.FoersteResultatReference = offset;

            fremsoegobjekthierarkiRequest request = new fremsoegobjekthierarkiRequest();
            request.FremsoegobjekthierarkiRequest1 = new FremsoegobjekthierarkiRequestType();
            request.FremsoegobjekthierarkiRequest1.FremsoegObjekthierarkiInput = input;
            request.FremsoegobjekthierarkiRequest1.AuthorityContext = new AuthorityContextType();
            request.FremsoegobjekthierarkiRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send request
            OrganisationSystemPortType channel = StubUtil.CreateChannel<OrganisationSystemPortType>(OrganisationSystemStubHelper.SERVICE, "FremsoegObjektHierarki", helper.CreatePort());

            try
            {
                var result = channel.fremsoegobjekthierarki(request);

                int statusCode = Int32.Parse(result.FremsoegobjekthierarkiResponse1.FremsoegObjekthierarkiOutput.StandardRetur.StatusKode);
                if (statusCode != 20)
                {
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "FremsoegObjektHierarki", OrganisationSystemStubHelper.SERVICE, result.FremsoegobjekthierarkiResponse1.FremsoegObjekthierarkiOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
                }

                var output = result.FremsoegobjekthierarkiResponse1.FremsoegObjekthierarkiOutput;
                if (log.IsDebugEnabled)
                {
                    log.Debug("Found: " + output.OrganisationEnheder.Length + " Enheder, " +
                                          output.Interessefaellesskaber.Length + " Interessefaellesskaber, " +
                                          output.ItSystemer.Length + " ItSystemer, " +
                                          output.Organisationer.Length + " Organisationer, " +
                                          output.OrganisationFunktioner.Length + " OrganisationFunktioner");
                }

                if (output.OrganisationEnheder.Length > 0)
                {
                    moreData = true;
                }

                List<OrgUnitRegWrapper> registrations = new List<OrgUnitRegWrapper>();

                var ous = result.FremsoegobjekthierarkiResponse1.FremsoegObjekthierarkiOutput.OrganisationEnheder;
                foreach (var ou in ous)
                {
                    string uuid = ou.ObjektType?.UUIDIdentifikator;

                    if (uuid == null)
                    {
                        log.Warn("OU in hierarchy does not have a uuid");
                    }
                    else if (ou.Registrering == null)
                    {
                        log.Warn("OU in hierarchy does not have a registration: " + uuid);
                    }
                    else
                    {
                        if (ou.Registrering.Length != 1)
                        {
                            log.Warn("OU in hierarchy does has more than one registration: " + uuid);
                        }

                        var reg = ou.Registrering[0];

                        if (StubUtil.GetMunicipalityOrganisationUUID().Equals(reg.RelationListe?.Tilhoerer?.ReferenceID?.Item))
                        {
                            registrations.Add(new OrgUnitRegWrapper() {
                                Uuid = uuid,
                                Registration = reg
                            });
                        }
                        else
                        {
                            log.Warn("Skipping OrgUnit with Tilhoerer relation unknown Organisation: " + reg.RelationListe?.Tilhoerer?.ReferenceID?.Item);
                        }
                    }
                }

                return registrations;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on OrganisationSystem", ex);
            }
        }
    }

    internal class OrgUnitRegWrapper
    {
        public string Uuid { get; set; }
        public dynamic Registration { get; set; }
    }
}
