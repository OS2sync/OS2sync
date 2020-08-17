using IntegrationLayer.Adresse;
using System;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class RawAdresseStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private AdresseStubHelper helper = new AdresseStubHelper();

        public importerResponse Create(ImportInputType inportInput)
        {
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
                return channel.importer(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on Adresse", ex);
            }
        }

        public retResponse Update(RetInputType1 input)
        {
            // send Ret request
            retRequest request = new retRequest();
            request.RetRequest1 = new RetRequestType();
            request.RetRequest1.RetInput = input;
            request.RetRequest1.AuthorityContext = new AuthorityContextType();
            request.RetRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                return channel.ret(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on Adresse", ex);
            }
        }

        public laesResponse Read(string uuid)
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
                return channel.laes(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on Adresse", ex);
            }
        }
    }
}