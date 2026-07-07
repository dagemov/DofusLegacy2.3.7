using Microsoft.AspNetCore.Mvc;
using OneLauncher.Api.Services;

namespace OneLauncher.Api.Controllers;

[ApiController]
[Route("api/launcher/electron-updates")]
public sealed class ElectronUpdatesController : ControllerBase
{
    private readonly IElectronUpdatesService _electronUpdatesService;
    private readonly ILogger<ElectronUpdatesController> _logger;

    public ElectronUpdatesController(
        IElectronUpdatesService electronUpdatesService,
        ILogger<ElectronUpdatesController> logger)
    {
        _electronUpdatesService = electronUpdatesService;
        _logger = logger;
    }

    [HttpGet("latest.yml")]
    public IActionResult GetLatestYaml()
    {
        if (!_electronUpdatesService.TryBuildLatestYaml(out string yaml))
        {
            _logger.LogWarning("latest.yml requested but electron update payload is unavailable.");
            return NotFound(new { error = "Electron update feed is not available." });
        }

        return Content(yaml, "text/yaml; charset=utf-8");
    }

    [HttpGet("{fileName}")]
    public IActionResult GetUpdateFile(string fileName)
    {
        ElectronUpdateFileResult? updateFile = _electronUpdatesService.GetUpdateFile(fileName);
        if (updateFile is null)
        {
            _logger.LogWarning("Electron update file {FileName} was requested but is not available", fileName);
            return NotFound();
        }

        _logger.LogInformation(
            "Serving electron update file {FileName} ({Bytes} bytes)",
            updateFile.FileName,
            updateFile.Content.Length);

        return File(updateFile.Content, updateFile.ContentType, updateFile.FileName);
    }
}
