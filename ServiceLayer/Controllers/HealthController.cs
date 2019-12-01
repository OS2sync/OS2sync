using Microsoft.AspNetCore.Mvc;

namespace Organisation.ServiceLayer
{
    [Route("/")]
    public class HealthController : Controller
    {
        public IActionResult Health()
        {
            return Ok();
        }
    }
}