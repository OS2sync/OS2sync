using System;
using System.ServiceModel;
using System.IO;
using System.Net;
using IntegrationLayer.Bruger;

namespace Organisation.IntegrationLayer
{
    internal class RawBrugerStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private BrugerStubHelper helper = new BrugerStubHelper();

        public importerResponse Create(ImportInputType importInput)
        {
            // construct request
            importerRequest request = new importerRequest();
            request.ImportInput = importInput;

            // send request
            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Importer");

            try
            {
                return channel.importerAsync(request).Result;
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
            request.RetInput = input;

            // send Ret request
            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Ret");

            try
            {
                return channel.retAsync(request).Result;
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
            request.LaesInput = laesInput;

            // send request
            BrugerPortType channel = StubUtil.CreateChannel<BrugerPortType>(BrugerStubHelper.SERVICE, "Laes");

            try
            {
                return channel.laesAsync(request).Result;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on Person", ex);
            }
        }
    }
}
