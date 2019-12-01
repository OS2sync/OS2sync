
namespace Organisation.IntegrationLayer
{
    internal class TestEnvironment : Environment
    {
        public string GetServicesBaseUrl()
        {
            return "https://exttest.serviceplatformen.dk/service/Organisation/";
        }
    }
}
