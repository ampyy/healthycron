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
            return View("Index");
        }

        [HttpGet("configuring-checks")]
        public IActionResult ConfiguringChecks()
        {
            return View("Index");
        }

        [HttpGet("notifications")]
        public IActionResult Notifications()
        {
            return View("Index");
        }

        [HttpGet("projects")]
        public IActionResult Projects()
        {
            return View("Index");
        }
    }
}
