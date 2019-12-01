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

        public List<OrgUnitRegWrapper> Read(string antal, string offset)
        {
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
                    string message = StubUtil.ConstructSoapErrorMessage(statusCode, "FremsoegObjektHierarki", OrganisationFunktionStubHelper.SERVICE, result.FremsoegobjekthierarkiResponse1.FremsoegObjekthierarkiOutput.StandardRetur.FejlbeskedTekst);
                    log.Error(message);
                    throw new SoapServiceException(message);
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

                        registrations.Add(new OrgUnitRegWrapper() {
                            Uuid = uuid,
                            Registration = ou.Registrering[0]
                        });
                    }
                }

                return registrations;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on OrganisationFunktion", ex);
            }
        }
    }

    internal class OrgUnitRegWrapper
    {
        public string Uuid { get; set; }
        public dynamic Registration { get; set; }
    }
}
