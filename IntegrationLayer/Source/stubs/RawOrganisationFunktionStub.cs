using IntegrationLayer.OrganisationFunktion;
using System;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class RawOrganisationFunktionStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrganisationFunktionStubHelper helper = new OrganisationFunktionStubHelper();

        public importerResponse Create(ImportInputType importInput)
        {
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
                return channel.importer(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on OrganisationFunktion", ex);
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

            // send request
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                return channel.ret(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on OrganisationFunktion", ex);
            }
        }


        public laesResponse Read(string uuid)
        {
            // construct request
            LaesInputType laesInput = new LaesInputType();
            laesInput.UUIDIdentifikator = uuid;

            laesRequest request = new laesRequest();
            request.LaesRequest1 = new LaesRequestType();
            request.LaesRequest1.LaesInput = laesInput;
            request.LaesRequest1.AuthorityContext = new AuthorityContextType();
            request.LaesRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send request
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "List", helper.CreatePort());

            try
            {
                return channel.laes(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on OrganisationFunktion", ex);
            }
        }

        public soegResponse Search(SoegInputType1 soegInput)
        {
            // construct request
            soegRequest request = new soegRequest();
            request.SoegRequest1 = new SoegRequestType();
            request.SoegRequest1.SoegInput = soegInput;
            request.SoegRequest1.AuthorityContext = new AuthorityContextType();
            request.SoegRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();


            // send request
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Soeg", helper.CreatePort());

            try
            {
                 return channel.soeg(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Soeg service on OrganisationFunktion", ex);
            }
        }

    }
}
