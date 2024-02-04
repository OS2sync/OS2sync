using Microsoft.AspNetCore.Mvc;
using Organisation.IntegrationLayer;

namespace Organisation.ServiceLayer
{
    public abstract class BaseController : Controller
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected string AuthorizeAndFetchCvr(string cvr, string apiKey)
        {
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
                throw new System.Exception("No CVR supplied or configured!");
            }

            OrganisationRegistryProperties.SetCurrentMunicipality(cvr);

            return OrganisationRegistryProperties.GetCurrentMunicipality();
        }
    }
}
