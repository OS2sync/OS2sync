using Organisation.IntegrationLayer;
using Microsoft.AspNetCore.Mvc;
using IntegrationLayer.OrganisationEnhed;

namespace Organisation.ServiceLayer
{
    [Route("api/[controller]")]
    public class RawOrganisationEnhedController : BaseController
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private RawOrganisationEnhedStub rawOrganisationEnhedStub = new RawOrganisationEnhedStub();

        [HttpPost]
        public IActionResult Create([FromBody] ImportInputType input, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            // setting it will revert to default if no value is supplied, so we can read a valid value afterwards
            OrganisationRegistryProperties.SetCurrentMunicipality(cvr);
            cvr = OrganisationRegistryProperties.GetCurrentMunicipality();

            importerResponse result = rawOrganisationEnhedStub.Create(input);
            return Ok(result);
        }

        [HttpPost("{uuid}")]
        public IActionResult Update(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey, [FromBody] RetInputType1 input)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            retResponse result = rawOrganisationEnhedStub.Update(input);
            return Ok(result);
        }

        [HttpGet("{uuid}")]
        public IActionResult Read(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if (AuthorizeAndFetchCvr(cvr, apiKey) == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(uuid))
            {
                return BadRequest("uuid is null");
            }

            laesResponse result = rawOrganisationEnhedStub.Read(uuid);
            return Ok(result);
        }

    }
}
