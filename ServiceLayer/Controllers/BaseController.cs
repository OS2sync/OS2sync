using Microsoft.AspNetCore.Mvc;
using Organisation.IntegrationLayer;

namespace Organisation.ServiceLayer
{
    public abstract class BaseController : Controller
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected string AuthorizeAndFetchCvr(string cvr, string apiKey)
        {
            if (!ValidApiKey(apiKey))
            {
                log.Warn("Rejected access because of wrong or missing ApiKey");
                return null;
            }

            string defaultCvr = OrganisationRegistryProperties.AppSettings.Cvr;
            if (!string.IsNullOrEmpty(defaultCvr)) {
                if (!string.IsNullOrEmpty(cvr) && !cvr.Equals(defaultCvr)) {
                    log.Warn("CVR supplied through HTTP HEADER (" + cvr + ") was overwritten by configured default (" + defaultCvr + ")");
                }

                cvr = defaultCvr;
            }

            // if no CVR is supplied or configured, stop execution
            if (string.IsNullOrEmpty(cvr)) {
                log.Warn("No CVR supplied or configured!");

                return null;
            }

            OrganisationRegistryProperties.SetCurrentMunicipality(cvr);

            return OrganisationRegistryProperties.GetCurrentMunicipality();
        }

        private bool ValidApiKey(string apiKey)
        {
            // if no ApiKey is configured, it is always allowed to call API
            if (string.IsNullOrEmpty(OrganisationRegistryProperties.AppSettings.ApiKey))
            {
                return true;
            }

            // check for match
            if (string.Equals(OrganisationRegistryProperties.AppSettings.ApiKey, apiKey))
            {
                return true;
            }

            return false;
        }
    }
}
