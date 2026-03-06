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

        [HttpGet("quick-start")]
        public IActionResult QuickStart()
        {
            return View();
        }

        [HttpGet("how-monitoring-works")]
        public IActionResult HowMonitoringWorks()
        {
            return View();
        }

        [HttpGet("monitors")]
        public IActionResult Monitors()
        {
            return View();
        }

        [HttpGet("dotnet-integration")]
        public IActionResult DotNetIntegration()
        {
            return View();
        }

        [HttpGet("best-practices")]
        public IActionResult BestPractices()
        {
            return View();
        }

        [HttpGet("api-reference")]
        public IActionResult ApiReference()
        {
            return View();
        }

        [HttpGet("cron-syntax")]
        public IActionResult CronSyntax()
        {
            return View();
        }

        [HttpGet("monitor-states")]
        public IActionResult MonitorStates()
        {
            return View();
        }

        [HttpGet("faq")]
        public IActionResult Faq()
        {
            return View();
        }

        [HttpGet("overview")]
        public IActionResult Overview() { return View(); }

        [HttpGet("configuration")]
        public IActionResult Configuration() { return View(); }

        [HttpGet("running-with-docker")]
        public IActionResult RunningWithDocker() { return View(); }

        [HttpGet("reliability-tips")]
        public IActionResult ReliabilityTips() { return View(); }

        [HttpGet("cron-syntax-cheatsheet")]
        public IActionResult CronSyntaxCheatsheet() { return View(); }

        [HttpGet("compared-to-sentry")]
        public IActionResult ComparedToSentry() { return View(); }

        [HttpGet("compared-to-cronitor")]
        public IActionResult ComparedToCronitor() { return View(); }

        [HttpGet("shell-scripts")]
        public IActionResult ShellScripts() { return View(); }

        [HttpGet("arduino")]
        public IActionResult Arduino() { return View(); }

        [HttpGet("network-routers")]
        public IActionResult NetworkRouters() { return View(); }

        [HttpGet("csharp")]
        public IActionResult CSharp() { return View(); }

        [HttpGet("email")]
        public IActionResult Email() { return View(); }

        [HttpGet("github-actions")]
        public IActionResult GitHubActions() { return View(); }

        [HttpGet("go")]
        public IActionResult Go() { return View(); }

        [HttpGet("javascript")]
        public IActionResult Javascript() { return View(); }

        [HttpGet("php")]
        public IActionResult Php() { return View(); }

        [HttpGet("powershell")]
        public IActionResult PowerShell() { return View(); }

        [HttpGet("python")]
        public IActionResult Python() { return View(); }

        [HttpGet("ruby")]
        public IActionResult Ruby() { return View(); }
    }
}

