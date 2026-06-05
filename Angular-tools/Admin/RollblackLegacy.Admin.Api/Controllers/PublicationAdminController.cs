using Microsoft.AspNetCore.Mvc;
using RollblackLegacy.Admin.Application.Abstractions.Publication;
using RollblackLegacy.Admin.Contracts.Publication;

namespace RollblackLegacy.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/v1/publication")]
public sealed class PublicationAdminController : ControllerBase
{
    private readonly IPublicationBackupStatusService _backupStatusService;

    public PublicationAdminController(IPublicationBackupStatusService backupStatusService)
    {
        _backupStatusService = backupStatusService;
    }

    /// <summary>
    /// Estado read-only de backups, validación y publish lane (Phase 4 — sin publicar ni restaurar).
    /// </summary>
    [HttpGet("backup-status")]
    [ProducesResponseType(typeof(PublicationBackupStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicationBackupStatusDto>> GetBackupStatus(CancellationToken cancellationToken)
    {
        var status = await _backupStatusService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }
}
