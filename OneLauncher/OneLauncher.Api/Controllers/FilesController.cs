using Microsoft.AspNetCore.Mvc;
using OneLauncher.Api.Services;

namespace OneLauncher.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IPackageFileService _packageFileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IPackageFileService packageFileService, ILogger<FilesController> logger)
    {
        _packageFileService = packageFileService;
        _logger = logger;
    }

    [HttpGet("{packageName}")]
    public IActionResult GetPackage(string packageName)
    {
        var packageFile = _packageFileService.GetPackage(packageName);
        if (packageFile is null)
        {
            _logger.LogWarning("Package {PackageName} was requested but is not available", packageName);
            return NotFound();
        }

        _logger.LogInformation(
            "Serving package {PackageName} with {PackageSize} bytes",
            packageFile.FileName,
            packageFile.Content.Length);

        return File(packageFile.Content, packageFile.ContentType, packageFile.FileName);
    }
}
