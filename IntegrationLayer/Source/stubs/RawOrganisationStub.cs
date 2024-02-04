﻿using IntegrationLayer.Organisation;
using System;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class RawOrganisationStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrganisationStubHelper helper = new OrganisationStubHelper();

        public importerResponse Create(ImportInputType importInput)
        {
            // construct request
            importerRequest request = new importerRequest();
            request.ImportInput = importInput;

            // send request
            OrganisationPortType channel = StubUtil.CreateChannel<OrganisationPortType>(OrganisationStubHelper.SERVICE, "Import");

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

            OrganisationPortType channel = StubUtil.CreateChannel<OrganisationPortType>(OrganisationStubHelper.SERVICE, "Ret");

            try
            {
                return channel.retAsync(request).Result;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on Organisation", ex);
            }
        }

        public laesResponse Read(string uuid)
        {
            LaesInputType laesInput = new LaesInputType();
            laesInput.UUIDIdentifikator = uuid;

            laesRequest request = new laesRequest();
            request.LaesInput = laesInput;

            OrganisationPortType channel = StubUtil.CreateChannel<OrganisationPortType>(OrganisationStubHelper.SERVICE, "Laes");

            try
            {
                return channel.laesAsync(request).Result;
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on Organisation", ex);
            }
        }
    }
}
