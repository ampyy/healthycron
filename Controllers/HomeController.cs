using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HealthyCron.Models;
using HealthyCron.Utilities.Service;

namespace HealthyCron.Controllers;

public class HomeController : Controller
{
    private readonly AxiomLogger _axiomLogger;

    public HomeController(AxiomLogger axiomLogger)
    {
        _axiomLogger = axiomLogger;
    }

    public IActionResult Index()
    {
        var user = HttpContext.Items["User"] as HealthyCron.Models.User;
        if (user != null) return Redirect("/dashboard");
        return View();
    }

    [Route("terms")]
    public IActionResult Terms()
    {
        return View();
    }

    [Route("privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var user = HttpContext.Items["User"] as HealthyCron.Models.User;
        if (user != null) return Redirect("/dashboard");
        return View();
    }
}
