using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HealthyCron.Models;

namespace HealthyCron.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var user = HttpContext.Items["User"] as HealthyCron.Models.User;
        if (user != null) return Redirect("/dashboard");
        return View();
    }
}
