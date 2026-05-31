using Microsoft.Extensions.Options;
using OneLauncher.Api.Contracts;
using OneLauncher.Api.Options;

namespace OneLauncher.Api.Services;

public sealed class LauncherManifestService : ILauncherManifestService
{
    private readonly IOptionsMonitor<LauncherManifestOptions> _options;
    private readonly ILogger<LauncherManifestService> _logger;

    public LauncherManifestService(
        IOptionsMonitor<LauncherManifestOptions> options,
        ILogger<LauncherManifestService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public LauncherManifestDto GetManifest()
    {
        var options = _options.CurrentValue;
        _logger.LogInformation(
            "Building launcher manifest {ManifestVersion} with {PackageCount} configured packages",
            options.Version,
            options.Packages.Count);

        return new LauncherManifestDto(
            options.Version,
            options.Packages
                .Select(package => new LauncherPackageDto(
                    package.Name,
                    package.Url,
                    package.Checksum,
                    package.Size))
                .ToArray(),
            new LauncherStatusDto(
                options.Launcher.MinimumVersion,
                options.Launcher.Status));
    }
}
