using System;
using System.ServiceModel;
using System.IO;
using System.Net;
using IntegrationLayer.Person;

namespace Organisation.IntegrationLayer
{
    internal class RawPersonStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private PersonStubHelper helper = new PersonStubHelper();

        public importerResponse Create(ImportInputType importInput)
        {
            // construct request
            importerRequest request = new importerRequest();
            request.ImporterRequest1 = new ImporterRequestType();
            request.ImporterRequest1.ImportInput = importInput;
            request.ImporterRequest1.AuthorityContext = new AuthorityContextType();
            request.ImporterRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send request
            PersonPortType channel = StubUtil.CreateChannel<PersonPortType>(PersonStubHelper.SERVICE, "Importer", helper.CreatePort());

            try
            {
                return channel.importer(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on Person", ex);
            }
        }

        public retResponse Update(RetInputType1 input)
        {
            // construct request
            retRequest request = new retRequest();
            request.RetRequest1 = new RetRequestType();
            request.RetRequest1.RetInput = input;
            request.RetRequest1.AuthorityContext = new AuthorityContextType();
            request.RetRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send Ret request
            PersonPortType channel = StubUtil.CreateChannel<PersonPortType>(PersonStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                return channel.ret(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on Person", ex);
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
            PersonPortType channel = StubUtil.CreateChannel<PersonPortType>(PersonStubHelper.SERVICE, "List", helper.CreatePort());

            try
            {
                return channel.laes(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on Person", ex);
            }
        }

    }
}
