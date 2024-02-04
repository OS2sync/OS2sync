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
            request.ImportInput = importInput;

            // send request
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Import");

            try
            {
                return channel.importerAsync(request).Result;
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
            request.RetInput = input;

            // send request
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Ret");

            try
            {
                return channel.retAsync(request).Result;
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
            request.LaesInput = laesInput;

            // send request
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "List");

            try
            {
                return channel.laesAsync(request).Result;
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
            request.SoegInput = soegInput;


            // send request
            OrganisationFunktionPortType channel = StubUtil.CreateChannel<OrganisationFunktionPortType>(OrganisationFunktionStubHelper.SERVICE, "Soeg");

            try
            {
                return channel.soegAsync(request).Result;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Soeg service on OrganisationFunktion", ex);
            }
        }
    }
}
