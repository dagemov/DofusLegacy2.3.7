using System.Xml.Linq;
using Microsoft.Extensions.Options;
using OneLauncher.Api.Options;

namespace OneLauncher.Api.Services;

public sealed record UpdatesXmlEntry(string Version, string File);

public sealed record UpdatesXmlManifest(
    string LatestVersion,
    IReadOnlyList<UpdatesXmlEntry> Entries);

public interface IUpdatesXmlCatalog
{
    bool TryGetManifest(out UpdatesXmlManifest manifest);

    bool IsPackageAllowed(string packageName);

    string? GetManifestPath();
}

public sealed class UpdatesXmlCatalog : IUpdatesXmlCatalog
{
    private readonly IOptionsMonitor<PackageStorageOptions> _options;
    private readonly ILogger<UpdatesXmlCatalog> _logger;

    public UpdatesXmlCatalog(
        IOptionsMonitor<PackageStorageOptions> options,
        ILogger<UpdatesXmlCatalog> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool TryGetManifest(out UpdatesXmlManifest manifest)
    {
        manifest = null!;

        string? xmlPath = ResolveManifestPath();
        if (xmlPath is null)
        {
            return false;
        }

        try
        {
            XDocument document = XDocument.Load(xmlPath);
            XElement? root = document.Root;

            if (root is null || !string.Equals(root.Name.LocalName, "updates", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Updates.xml has invalid root element: {Path}", xmlPath);
                return false;
            }

            List<UpdatesXmlEntry> entries = root.Elements("update")
                .Select(ParseUpdateElement)
                .Where(entry => entry is not null)
                .Cast<UpdatesXmlEntry>()
                .ToList();

            if (entries.Count == 0)
            {
                _logger.LogWarning("Updates.xml contains no valid update entries: {Path}", xmlPath);
                return false;
            }

            string latestVersion = entries
                .OrderBy(entry => entry, Comparer<UpdatesXmlEntry>.Create(
                    (left, right) => CompareVersions(left.Version, right.Version)))
                .Last()
                .Version;

            manifest = new UpdatesXmlManifest(latestVersion, entries);
            _logger.LogInformation(
                "Loaded Updates.xml from {Path} with {Count} entries (latest {LatestVersion})",
                xmlPath,
                entries.Count,
                latestVersion);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Updates.xml from {Path}", xmlPath);
            return false;
        }
    }

    public bool IsPackageAllowed(string packageName)
    {
        if (!TryGetManifest(out UpdatesXmlManifest manifest))
        {
            return false;
        }

        return manifest.Entries.Any(entry =>
            string.Equals(entry.File, packageName, StringComparison.OrdinalIgnoreCase));
    }

    public string? GetManifestPath() => ResolveManifestPath();

    public string? ResolveManifestPath()
    {
        PackageStorageOptions options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.RootPath))
        {
            return null;
        }

        string fileName = string.IsNullOrWhiteSpace(options.ManifestFileName)
            ? "Updates.xml"
            : options.ManifestFileName;

        string manifestPath = Path.Combine(options.RootPath, fileName);
        return File.Exists(manifestPath) ? manifestPath : null;
    }

    private static UpdatesXmlEntry? ParseUpdateElement(XElement element)
    {
        string? version = element.Element("version")?.Value?.Trim();
        string? file = element.Element("file")?.Value?.Trim();

        if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(file))
        {
            return null;
        }

        return new UpdatesXmlEntry(version, Path.GetFileName(file));
    }

    private static int CompareVersions(string left, string right)
    {
        int[] leftParts = left.Split('.').Select(part => int.TryParse(part, out int value) ? value : 0).ToArray();
        int[] rightParts = right.Split('.').Select(part => int.TryParse(part, out int value) ? value : 0).ToArray();
        int maxLength = Math.Max(leftParts.Length, rightParts.Length);

        for (int index = 0; index < maxLength; index++)
        {
            int leftValue = index < leftParts.Length ? leftParts[index] : 0;
            int rightValue = index < rightParts.Length ? rightParts[index] : 0;

            if (leftValue != rightValue)
            {
                return leftValue.CompareTo(rightValue);
            }
        }

        return 0;
    }
}
