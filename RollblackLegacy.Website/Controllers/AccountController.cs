using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Website.Application.Abstractions;
using RollblackLegacy.Website.Contracts.Account;
using RollblackLegacy.Website.Infrastructure;

namespace RollblackLegacy.Website.Controllers;

public sealed class AccountController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IAccountRegistrationService _registrationService;
    private readonly IAccountLoginService _loginService;

    public AccountController(
        IConfiguration configuration,
        IAccountRegistrationService registrationService,
        IAccountLoginService loginService)
    {
        _configuration = configuration;
        _registrationService = registrationService;
        _loginService = loginService;
    }

    [HttpGet("/account/register")]
    public IActionResult Register()
    {
        ViewData["ActiveNav"] = "register";
        return View(WebsiteViewModelFactory.CreateRegisterPage(_configuration));
    }

    [HttpPost("/account/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterAccountInputModel form,
        CancellationToken cancellationToken)
    {
        ViewData["ActiveNav"] = "register";

        if (!ModelState.IsValid)
            return RenderRegister(WebsiteViewModelFactory.CreateRegisterPage(_configuration, form));

        string? remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        RegisterAccountResultViewModel result = await _registrationService.RegisterAsync(
            form,
            remoteIp,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Title.Contains("Nombre de cuenta", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(RegisterAccountInputModel.Username), result.Message);
            else if (result.Title.Contains("Correo", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(RegisterAccountInputModel.Email), result.Message);
            else
                ModelState.AddModelError(string.Empty, result.Message);
        }

        RegisterAccountPageViewModel viewModel = WebsiteViewModelFactory.CreateRegisterPage(
            _configuration,
            result.Succeeded ? new RegisterAccountInputModel() : form,
            result);

        return RenderRegister(viewModel);
    }

    [HttpGet("/account/login")]
    public IActionResult Login()
    {
        ViewData["ActiveNav"] = "login";
        return View(WebsiteViewModelFactory.CreateLoginPage(_configuration));
    }

    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginAccountInputModel form,
        CancellationToken cancellationToken)
    {
        ViewData["ActiveNav"] = "login";

        if (!ModelState.IsValid)
            return RenderLogin(WebsiteViewModelFactory.CreateLoginPage(_configuration, form));

        LoginAccountResultViewModel result = await _loginService.LoginAsync(form, cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return RenderLogin(WebsiteViewModelFactory.CreateLoginPage(_configuration, form, result));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.AccountId?.ToString() ?? "0"),
            new(ClaimTypes.Name, result.Username ?? form.Username),
            new("nickname", result.Nickname ?? result.Username ?? form.Username),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            });

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/account/check-username")]
    public async Task<IActionResult> CheckUsername(
        [FromQuery] string? username,
        CancellationToken cancellationToken)
    {
        UsernameAvailabilityViewModel result = await _registrationService.CheckUsernameAvailabilityAsync(
            username,
            cancellationToken);

        return PartialView("_UsernameAvailability", result);
    }

    private IActionResult RenderRegister(RegisterAccountPageViewModel viewModel)
    {
        if (Request.IsHtmxRequest())
            return PartialView("~/Views/Account/_RegisterPanelHtmx.cshtml", viewModel);

        return View(viewModel);
    }

    private IActionResult RenderLogin(LoginAccountPageViewModel viewModel)
    {
        if (Request.IsHtmxRequest())
            return PartialView("~/Views/Account/_LoginPanelHtmx.cshtml", viewModel);

        return View(viewModel);
    }
}
