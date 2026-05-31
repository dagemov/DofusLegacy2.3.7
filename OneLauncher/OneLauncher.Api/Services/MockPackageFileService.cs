using System.IO.Compression;
using Microsoft.Extensions.Options;
using OneLauncher.Api.Options;

namespace OneLauncher.Api.Services;

public sealed class MockPackageFileService : IPackageFileService
{
    private const string ContentType = "application/octet-stream";

    private readonly IOptionsMonitor<LauncherManifestOptions> _options;
    private readonly ILogger<MockPackageFileService> _logger;

    public MockPackageFileService(
        IOptionsMonitor<LauncherManifestOptions> options,
        ILogger<MockPackageFileService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public PackageFileResult? GetPackage(string packageName)
    {
        var package = _options.CurrentValue.Packages
            .FirstOrDefault(candidate => string.Equals(
                candidate.Name,
                packageName,
                StringComparison.OrdinalIgnoreCase));

        if (package is null)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("bootstrap-api-smoke.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.WriteLine("OneLauncher API bootstrap package.");
            writer.WriteLine($"Package: {package.Name}");
            writer.WriteLine($"GeneratedUtc: {DateTimeOffset.UtcNow:O}");
        }

        _logger.LogInformation("Generated mock package {PackageName}", package.Name);
        return new PackageFileResult(package.Name, ContentType, stream.ToArray());
    }
}
