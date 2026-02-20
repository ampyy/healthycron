using Microsoft.AspNetCore.Mvc;

namespace HealthyCron.Controllers
{
    [Route("docs")]
    public class DocsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("pinging-api")]
        public IActionResult PingingApi()
        {
            return View();
        }

        [HttpGet("configuring-checks")]
        public IActionResult ConfiguringChecks()
        {
            return View();
        }

        [HttpGet("notifications")]
        public IActionResult Notifications()
        {
            return View();
        }

        [HttpGet("projects")]
        public IActionResult Projects()
        {
            return View();
        }
    }
}

