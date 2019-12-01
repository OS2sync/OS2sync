using Microsoft.AspNetCore.Mvc;
using Organisation.IntegrationLayer;

namespace Organisation.ServiceLayer
{
    public abstract class BaseController : Controller
    {
        protected string AuthorizeAndFetchCvr(string cvr, string apiKey)
        {
            if (!ApiKeyFilter.ValidApiKey(apiKey))
            {
                return null;
            }

            // setting it will revert to default if no value is supplied, so we can read a valid value afterwards
            OrganisationRegistryProperties.SetCurrentMunicipality(cvr);

            return OrganisationRegistryProperties.GetCurrentMunicipality();
        }
    }
}
