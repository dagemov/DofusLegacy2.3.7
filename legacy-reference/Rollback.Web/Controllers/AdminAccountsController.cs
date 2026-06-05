using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Rollback.Accounts.Services;

namespace Rollback.Web.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("admin/users")]
public sealed class AdminAccountsController : Controller
{
    private readonly IAccountPortalService _accountPortalService;

    public AdminAccountsController(IAccountPortalService accountPortalService) =>
        _accountPortalService = accountPortalService;

    [HttpPost("{accountId:int}/ban")]
    public async Task<IActionResult> Ban(int accountId, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        if (accountId == GetCurrentAccountId())
            return RedirectWithStatus(returnUrl, "No puedes banear tu propia cuenta desde el portal.");

        var updated = await _accountPortalService.SetBanStateAsync(accountId, banned: true, cancellationToken: cancellationToken);
        return RedirectWithStatus(returnUrl, updated ? "Cuenta baneada por 7 dias." : "No se pudo banear la cuenta.");
    }

    [HttpPost("{accountId:int}/unban")]
    public async Task<IActionResult> Unban(int accountId, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        var updated = await _accountPortalService.SetBanStateAsync(accountId, banned: false, cancellationToken: cancellationToken);
        return RedirectWithStatus(returnUrl, updated ? "Cuenta desbaneada." : "No se pudo actualizar la cuenta.");
    }

    [HttpPost("/admin/characters/{characterId:int}/kamas")]
    public async Task<IActionResult> UpdateKamas(int characterId, [FromForm] int kamas, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        var updated = await _accountPortalService.SetCharacterKamasAsync(characterId, kamas, cancellationToken);
        return RedirectWithStatus(returnUrl, updated ? "Kamas actualizados." : "No se pudieron actualizar los kamas.");
    }

    [HttpPost("/admin/characters/{characterId:int}/level")]
    public async Task<IActionResult> UpdateLevel(int characterId, [FromForm] byte level, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        var updated = await _accountPortalService.SetCharacterLevelAsync(characterId, level, cancellationToken);
        return RedirectWithStatus(returnUrl, updated ? "Nivel actualizado." : "No se pudo actualizar el nivel.");
    }

    private int GetCurrentAccountId()
    {
        var rawId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(rawId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var accountId)
            ? accountId
            : 0;
    }

    private IActionResult RedirectWithStatus(string? returnUrl, string status)
    {
        var target = SanitizeLocalReturnUrl(returnUrl, "/admin/users");
        return Redirect(QueryHelpers.AddQueryString(target, "status", status));
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
