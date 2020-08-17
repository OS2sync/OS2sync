using Organisation.IntegrationLayer;
using Microsoft.AspNetCore.Mvc;
using IntegrationLayer.OrganisationFunktion;

namespace Organisation.ServiceLayer
{
    [Route("api/[controller]")]
    public class RawOrganisationFunktionController : BaseController
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private RawOrganisationFunktionStub rawOrganisationFunctionStub = new RawOrganisationFunktionStub();

        [HttpPost]
        public IActionResult Create([FromBody] ImportInputType input, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            importerResponse result = rawOrganisationFunctionStub.Create(input);

            return Ok(result);
        }

        [HttpPost("{uuid}")]
        public IActionResult Update([FromHeader] string cvr, [FromHeader] string apiKey, [FromBody] RetInputType1 input)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            retResponse result = rawOrganisationFunctionStub.Update(input);

            return Ok(result);
        }

        [HttpGet("{uuid}")]
        public IActionResult Read(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(uuid))
            {
                return BadRequest("uuid is null or empty");
            }

            var result = rawOrganisationFunctionStub.Read(uuid);

            return Ok(result);
        }

        [HttpPost]
        [Route("search")]
        public IActionResult Search([FromHeader] string cvr, [FromHeader] string apiKey, [FromBody] SoegInputType1 input)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            soegResponse result = rawOrganisationFunctionStub.Search(input);

            return Ok(result);
        }

    }
}
