using Organisation.IntegrationLayer;
using Microsoft.AspNetCore.Mvc;
using IntegrationLayer.Person;

namespace Organisation.ServiceLayer
{
    [Route("api/[controller]")]
    public class RawPersonController : BaseController
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private RawPersonStub rawPersonStub = new RawPersonStub();

        [HttpPost]
        public IActionResult Create([FromBody] ImportInputType input, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            importerResponse result = rawPersonStub.Create(input);

            return Ok(result);
        }

        [HttpPost("{uuid}")]
        public IActionResult Update([FromHeader] string cvr, [FromHeader] string apiKey, [FromBody] RetInputType1 input)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            retResponse result = rawPersonStub.Update(input);

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

            var result = rawPersonStub.Read(uuid);

            return Ok(result);
        }

    }
}
