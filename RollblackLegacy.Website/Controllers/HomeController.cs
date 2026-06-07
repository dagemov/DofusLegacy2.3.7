using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Website.Infrastructure;

namespace RollblackLegacy.Website.Controllers;

public sealed class HomeController : Controller
{
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("/")]
    [HttpGet("/home")]
    public IActionResult Index()
    {
        ViewData["ActiveNav"] = "home";
        var page = WebsiteViewModelFactory.CreateHomePage(_configuration);
        ViewData["BetaStatusLabel"] = page.BetaStatusLabel;
        return View(page);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
