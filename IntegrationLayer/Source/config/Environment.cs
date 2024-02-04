namespace Organisation.IntegrationLayer
{
    internal interface Environment
    {
        public string GetStsEndpointAddress();
        public string GetStsEntityIdentifier();
        public string GetWspEndpointBaseUrl();
        public string GetWspEndpointID();
    }
}
