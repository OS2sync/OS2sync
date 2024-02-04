
namespace Organisation.IntegrationLayer
{
    internal class ServiceSettings
    {
        public string WspEndpointBaseUrl { get; set; } = "https://organisation.stoettesystemerne.dk/organisation/";
        public string WspEndpointID { get; set; } = "http://stoettesystemerne.dk/service/organisation/3";
        public string WspCertificateLocation { get; set; }
    }
}
