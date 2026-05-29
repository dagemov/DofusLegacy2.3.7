using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Website.Application.Abstractions;
using RollblackLegacy.Website.Contracts.Account;
using RollblackLegacy.Website.Infrastructure;

namespace RollblackLegacy.Website.Controllers;

public sealed class AccountController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IAccountRegistrationService _registrationService;

    public AccountController(
        IConfiguration configuration,
        IAccountRegistrationService registrationService)
    {
        _configuration = configuration;
        _registrationService = registrationService;
    }

    [HttpGet("/account/register")]
    public IActionResult Register()
    {
        ViewData["BodyClass"] = "page-register";
        return View(WebsiteViewModelFactory.CreateRegisterPage(_configuration));
    }

    [HttpPost("/account/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterAccountInputModel form,
        CancellationToken cancellationToken)
    {
        ViewData["BodyClass"] = "page-register";

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
            return PartialView("~/Views/Shared/Organisms/_RegisterPanel.cshtml", viewModel);

        return View(viewModel);
    }
}
