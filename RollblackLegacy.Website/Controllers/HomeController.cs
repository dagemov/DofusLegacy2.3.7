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
        ViewData["BodyClass"] = "page-home";
        return View(WebsiteViewModelFactory.CreateHomePage(_configuration));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
