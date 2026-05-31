using Microsoft.Extensions.Options;
using OneLauncher.Api.Options;

namespace OneLauncher.Api.Services;

public sealed class DiskPackageFileService : IPackageFileService
{
    private const string ContentType = "application/octet-stream";
    private readonly IOptionsMonitor<PackageStorageOptions> _storageOptions;
    private readonly IOptionsMonitor<LauncherManifestOptions> _manifestOptions;
    private readonly ILogger<DiskPackageFileService> _logger;

    public DiskPackageFileService(
        IOptionsMonitor<PackageStorageOptions> storageOptions,
        IOptionsMonitor<LauncherManifestOptions> manifestOptions,
        ILogger<DiskPackageFileService> logger)
    {
        _storageOptions = storageOptions;
        _manifestOptions = manifestOptions;
        _logger = logger;
    }

    public PackageFileResult? GetPackage(string packageName)
    {
        if (!_manifestOptions.CurrentValue.Packages.Any(package =>
                string.Equals(package.Name, packageName, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        string rootPath = _storageOptions.CurrentValue.RootPath;
        if (string.IsNullOrWhiteSpace(rootPath))
            return null;

        string safeName = Path.GetFileName(packageName);
        string filePath = Path.Combine(rootPath, safeName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Package file missing on disk: {FilePath}", filePath);
            return null;
        }

        byte[] content = File.ReadAllBytes(filePath);
        _logger.LogInformation("Serving package {PackageName} from disk ({Bytes} bytes)", safeName, content.Length);
        return new PackageFileResult(safeName, ContentType, content);
    }
}
