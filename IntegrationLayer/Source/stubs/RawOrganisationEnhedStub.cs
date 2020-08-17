﻿using IntegrationLayer.OrganisationEnhed;
using System;
using System.IO;
using System.Net;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class RawOrganisationEnhedStub
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrganisationEnhedStubHelper helper = new OrganisationEnhedStubHelper();

        public importerResponse Create(ImportInputType input)
        {
            // construct request
            importerRequest request = new importerRequest();
            request.ImporterRequest1 = new ImporterRequestType();
            request.ImporterRequest1.ImportInput = input;
            request.ImporterRequest1.AuthorityContext = new AuthorityContextType();
            request.ImporterRequest1.AuthorityContext.MunicipalityCVR = OrganisationRegistryProperties.GetCurrentMunicipality();

            // send request
            OrganisationEnhedPortType channel = StubUtil.CreateChannel<OrganisationEnhedPortType>(OrganisationEnhedStubHelper.SERVICE, "Importer", helper.CreatePort());

            try
            {
                return channel.importer(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Importer service on OrganisationEnhed", ex);
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

            // send request
            OrganisationEnhedPortType channel = StubUtil.CreateChannel<OrganisationEnhedPortType>(OrganisationEnhedStubHelper.SERVICE, "Ret", helper.CreatePort());

            try
            {
                return channel.ret(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Ret service on OrganisationEnhed", ex);
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
            OrganisationEnhedPortType channel = StubUtil.CreateChannel<OrganisationEnhedPortType>(OrganisationEnhedStubHelper.SERVICE, "Laes", helper.CreatePort());

            try
            {
                return channel.laes(request);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is IOException || ex is TimeoutException || ex is WebException)
            {
                throw new ServiceNotFoundException("Failed to establish connection to the Laes service on OrganisationEnhed", ex);
            }
        }

    }
}
