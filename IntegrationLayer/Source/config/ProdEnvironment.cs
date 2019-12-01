
namespace Organisation.IntegrationLayer
{
    internal class ProdEnvironment : Environment
    {
        public string GetServicesBaseUrl()
        {
            return "https://prod.serviceplatformen.dk/service/Organisation/";
        }
    }
}
