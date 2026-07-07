using Microsoft.Extensions.Options;
using OneLauncher.Api.Contracts;
using OneLauncher.Api.Options;

namespace OneLauncher.Api.Services;

public sealed class LauncherManifestService : ILauncherManifestService
{
    private readonly IOptionsMonitor<LauncherManifestOptions> _options;
    private readonly IUpdatesXmlCatalog _updatesXmlCatalog;
    private readonly IOptionsMonitor<PackageStorageOptions> _packageStorageOptions;
    private readonly ILogger<LauncherManifestService> _logger;

    public LauncherManifestService(
        IOptionsMonitor<LauncherManifestOptions> options,
        IUpdatesXmlCatalog updatesXmlCatalog,
        IOptionsMonitor<PackageStorageOptions> packageStorageOptions,
        ILogger<LauncherManifestService> logger)
    {
        _options = options;
        _updatesXmlCatalog = updatesXmlCatalog;
        _packageStorageOptions = packageStorageOptions;
        _logger = logger;
    }

    public LauncherManifestDto GetManifest()
    {
        if (_updatesXmlCatalog.TryGetManifest(out UpdatesXmlManifest xmlManifest))
        {
            return BuildFromUpdatesXml(xmlManifest);
        }

        return BuildFromAppSettings();
    }

    private LauncherManifestDto BuildFromUpdatesXml(UpdatesXmlManifest xmlManifest)
    {
        PackageStorageOptions storage = _packageStorageOptions.CurrentValue;
        string publicBaseUrl = storage.PublicBaseUrl.TrimEnd('/');
        LauncherManifestOptions options = _options.CurrentValue;

        LauncherUpdateEntryDto[] updates = xmlManifest.Entries
            .Select(entry => CreateUpdateEntry(entry, storage.RootPath, publicBaseUrl))
            .ToArray();

        LauncherPackageDto[] packages = updates
            .GroupBy(update => update.File, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .Select(update => new LauncherPackageDto(
                update.File,
                update.Url,
                update.Checksum,
                update.Size))
            .ToArray();

        _logger.LogInformation(
            "Building launcher manifest from Updates.xml {ManifestVersion} with {PackageCount} packages",
            xmlManifest.LatestVersion,
            packages.Length);

        return new LauncherManifestDto(
            xmlManifest.LatestVersion,
            packages,
            new LauncherStatusDto(
                options.Launcher.MinimumVersion,
                options.Launcher.Status),
            updates,
            "updates-xml");
    }

    private LauncherManifestDto BuildFromAppSettings()
    {
        LauncherManifestOptions options = _options.CurrentValue;
        _logger.LogInformation(
            "Building launcher manifest from appsettings {ManifestVersion} with {PackageCount} configured packages",
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
                options.Launcher.Status),
            null,
            "appsettings");
    }

    private static LauncherUpdateEntryDto CreateUpdateEntry(
        UpdatesXmlEntry entry,
        string rootPath,
        string publicBaseUrl)
    {
        string safeName = Path.GetFileName(entry.File);
        string filePath = Path.Combine(rootPath, safeName);
        long size = 0;

        if (File.Exists(filePath))
        {
            size = new FileInfo(filePath).Length;
        }

        return new LauncherUpdateEntryDto(
            entry.Version,
            safeName,
            $"{publicBaseUrl}/api/files/{Uri.EscapeDataString(safeName)}",
            "TEMP",
            size);
    }
}
