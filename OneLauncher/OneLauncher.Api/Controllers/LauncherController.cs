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
            "Returning launcher manifest {ManifestVersion} with {PackageCount} packages (source={Source})",
            manifest.Version,
            manifest.Packages.Count,
            manifest.ManifestSource);

        return Ok(manifest);
    }

    [HttpGet("updates.xml")]
    public IActionResult GetUpdatesXml([FromServices] IUpdatesXmlCatalog catalog)
    {
        string? manifestPath = catalog.GetManifestPath();
        if (manifestPath is null)
        {
            _logger.LogWarning("Updates.xml was requested but no manifest file exists on disk.");
            return NotFound();
        }

        return PhysicalFile(manifestPath, "application/xml", "Updates.xml");
    }
}
