using Organisation.IntegrationLayer;

namespace Organisation.ServiceLayer
{
    public class ApiKeyFilter
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static OrganisationRegistryProperties properties = OrganisationRegistryProperties.GetInstance();

        public static bool ValidApiKey(string apiKey)
        {
            // if no ApiKey is configured, it is always allowed to call API
            if (string.IsNullOrEmpty(properties.ApiKey))
            {
                return true;
            }

            // check for match
            if (properties.ApiKey.Equals(apiKey))
            {
                return true;
            }

            log.Warn("Rejected access because of wrong or missing ApiKey");

            return false;
        }
    }
}