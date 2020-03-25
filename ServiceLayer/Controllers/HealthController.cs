using Microsoft.AspNetCore.Mvc;

namespace Organisation.ServiceLayer
{
    [Route("/manage/health")]
    public class HealthController : Controller
    {
        public IActionResult Health()
        {
            return Ok();
        }
    }
}