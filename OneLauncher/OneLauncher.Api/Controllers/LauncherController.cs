using Microsoft.AspNetCore.Mvc;
using OneLauncher.Api.Contracts;
using OneLauncher.Api.Services;

namespace OneLauncher.Api.Controllers;

[ApiController]
[Route("api/launcher")]
public sealed class LauncherController : ControllerBase
{
    private readonly ILauncherManifestService _manifestService;
    private readonly ILogger<LauncherController> _logger;

    public LauncherController(
        ILauncherManifestService manifestService,
        ILogger<LauncherController> logger)
    {
        _manifestService = manifestService;
        _logger = logger;
    }

    [HttpGet("manifest")]
    public ActionResult<LauncherManifestDto> GetManifest()
    {
        var manifest = _manifestService.GetManifest();
        _logger.LogInformation(
            "Returning launcher manifest {ManifestVersion} with {PackageCount} packages",
            manifest.Version,
            manifest.Packages.Count);

        return Ok(manifest);
    }
}
