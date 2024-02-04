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
            request.ImportInput = inportInput;

            // send request
            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Importer");

            try
            {
                return channel.importerAsync(request).Result;
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
            request.RetInput = input;

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Ret");

            try
            {
                return channel.retAsync(request).Result;
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
            request.LaesInput = laesInput;

            AdressePortType channel = StubUtil.CreateChannel<AdressePortType>(AdresseStubHelper.SERVICE, "Laes");

            try
            {
                return channel.laesAsync(request).Result;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on Adresse", ex);
            }
        }
    }
}