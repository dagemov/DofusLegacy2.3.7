using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rollback.Accounts.Models;
using Rollback.Accounts.Services;

namespace Rollback.Web.Controllers;

[AllowAnonymous]
[ApiController]
[Route("auth")]
public sealed class AuthController : Controller
{
    private readonly IAccountPortalService _accountPortalService;

    public AuthController(IAccountPortalService accountPortalService) =>
        _accountPortalService = accountPortalService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] string login, [FromForm] string password, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        var account = await _accountPortalService.AuthenticateAsync(login, password, cancellationToken);
        if (account is null)
            return RedirectWithMessage("/login", "Credenciales invalidas.", returnUrl);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(account),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            });

        return LocalRedirect(SanitizeLocalReturnUrl(returnUrl, "/account"));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromForm] string login,
        [FromForm] string nickname,
        [FromForm] string password,
        [FromForm] string confirmPassword,
        [FromForm] string? returnUrl,
        CancellationToken cancellationToken)
    {
        var result = await _accountPortalService.RegisterAsync(
            new AccountRegistrationRequest(login, nickname, password, confirmPassword),
            cancellationToken);

        if (!result.Succeeded || result.Account is null)
            return RedirectWithMessage("/register", result.ErrorMessage ?? "No se pudo crear la cuenta.", returnUrl);

        var account = result.Account;
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CreatePrincipal(account),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            });

        return LocalRedirect(SanitizeLocalReturnUrl(returnUrl, "/account"));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/login?loggedOut=true");
    }

    private IActionResult RedirectWithMessage(string path, string message, string? returnUrl)
    {
        var query = new Dictionary<string, string?>
        {
            ["error"] = message,
            ["returnUrl"] = SanitizeLocalReturnUrl(returnUrl, "/account"),
        };

        return Redirect(QueryHelpers.AddQueryString(path, query));
    }

    private static ClaimsPrincipal CreatePrincipal(PortalAccountSummary account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString(CultureInfo.InvariantCulture)),
            new("AccountId", account.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, account.Login),
            new(ClaimTypes.Role, account.Role.ToString()),
        };

        if (!string.IsNullOrWhiteSpace(account.Nickname))
            claims.Add(new Claim("nickname", account.Nickname));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static string SanitizeLocalReturnUrl(string? returnUrl, string fallback)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return fallback;

        return returnUrl.StartsWith("/", StringComparison.Ordinal) && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            ? returnUrl
            : fallback;
    }
}
