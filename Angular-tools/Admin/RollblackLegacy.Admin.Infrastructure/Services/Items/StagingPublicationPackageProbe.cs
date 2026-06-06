using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Infrastructure.Services.Items;

public sealed class StagingPublicationPackageProbe : IStagingPublicationPackageProbe
{
    private const string Phase3cFolder = "publication-package-phase3c";
    private const string Phase3bFolder = "publication-phase3b";

    private readonly string _contentRootPath;

    public StagingPublicationPackageProbe(IHostEnvironment hostEnvironment)
    {
        _contentRootPath = hostEnvironment.ContentRootPath;
    }

    public StagingPublicationPackageProbeResult Probe(int itemId)
    {
        var repoRoot = ResolveRepoRoot(_contentRootPath);
        if (repoRoot is null)
        {
            return Empty(StagingPublicationPackageStatuses.NoPackageGenerated);
        }

        var packageDirectory = FindPackageDirectory(repoRoot, itemId);
        if (packageDirectory is null)
        {
            return new StagingPublicationPackageProbeResult(
                StagingPublicationPackageStatuses.NoPackageGenerated,
                null,
                null,
                null,
                [],
                [],
                [
                    "Generar paquete con: dotnet run --project Infrastructure/scripts/ClientItemPublicationPipeline/ClientItemPublicationPipeline.csproj -- --mode stage-item-publication --item-id "
                    + itemId
                    + " --output Infrastructure/staging-client/publication-package-phase3c/"
                    + itemId
                ]);
        }

        var relativePath = ToRepoRelative(repoRoot, packageDirectory);
        var manifestPath = Path.Combine(packageDirectory, "publication-package-manifest.json");
        var validationPath = Path.Combine(packageDirectory, "validation-report.json");
        var manifest = TryReadManifest(manifestPath);
        var validation = TryReadValidation(validationPath);

        var packageId = manifest?.PackageId;
        var validationStatus = validation?.ValidationStatus ?? manifest?.ValidationStatus;
        var blocking = new List<string>();
        var warnings = new List<string>();
        var nextSteps = new List<string>();

        if (validation?.BlockingReasons is { Count: > 0 })
        {
            blocking.AddRange(validation.BlockingReasons);
        }
        else if (manifest?.BlockingReasons is { Count: > 0 })
        {
            blocking.AddRange(manifest.BlockingReasons);
        }

        if (validation?.Warnings is { Count: > 0 })
        {
            warnings.AddRange(validation.Warnings);
        }
        else if (manifest?.Warnings is { Count: > 0 })
        {
            warnings.AddRange(manifest.Warnings);
        }

        if (validation?.NextManualSteps is { Count: > 0 })
        {
            nextSteps.AddRange(validation.NextManualSteps);
        }
        else if (manifest?.NextManualSteps is { Count: > 0 })
        {
            nextSteps.AddRange(manifest.NextManualSteps);
        }

        string stagingStatus;
        if (IsReadyValidation(validationStatus))
        {
            stagingStatus = StagingPublicationPackageStatuses.ReadyForControlledPublish;
            if (nextSteps.Count == 0)
            {
                nextSteps.Add("Phase 4: aplicar patch solo en copia backup del cliente; no publicar a Client2.3.7 original.");
            }
        }
        else if (!File.Exists(validationPath) && !IsReadyValidation(validationStatus))
        {
            stagingStatus = StagingPublicationPackageStatuses.NeedsValidation;
            nextSteps.Insert(
                0,
                "Ejecutar: --mode validate-publication-package --package "
                + relativePath);
        }
        else if (string.Equals(validationStatus, StagingPublicationPackageValidationStatuses.InvalidStagingPackage, StringComparison.Ordinal)
            || string.Equals(validationStatus, StagingPublicationPackageValidationStatuses.BlockedValidation, StringComparison.Ordinal))
        {
            stagingStatus = StagingPublicationPackageStatuses.NeedsValidation;
        }
        else
        {
            stagingStatus = StagingPublicationPackageStatuses.PackageAvailableInStaging;
            if (nextSteps.Count == 0)
            {
                nextSteps.Add("Validar paquete staging antes de patch controlado.");
            }
        }

        return new StagingPublicationPackageProbeResult(
            stagingStatus,
            relativePath,
            packageId,
            validationStatus,
            blocking,
            warnings,
            nextSteps);
    }

    private static bool IsReadyValidation(string? status) =>
        string.Equals(status, StagingPublicationPackageValidationStatuses.ValidStagingPackage, StringComparison.Ordinal)
        || string.Equals(status, StagingPublicationPackageValidationStatuses.ReadyForControlledPublish, StringComparison.Ordinal);

    private static StagingPublicationPackageProbeResult Empty(string status) =>
        new(status, null, null, null, [], [], []);

    private static string? FindPackageDirectory(string repoRoot, int itemId)
    {
        foreach (var folder in new[] { Phase3cFolder, Phase3bFolder })
        {
            var candidate = Path.Combine(repoRoot, "Infrastructure", "staging-client", folder, itemId.ToString());
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static PackageManifestSnapshot? TryReadManifest(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<PackageManifestSnapshot>(stream, JsonProbeOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PackageValidationSnapshot? TryReadValidation(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<PackageValidationSnapshot>(stream, JsonProbeOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ResolveRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Angular-tools", "Admin"))
                && Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string ToRepoRelative(string repoRoot, string absolutePath)
    {
        var relative = Path.GetRelativePath(repoRoot, absolutePath);
        return relative.Replace('\\', '/');
    }

    private static readonly JsonSerializerOptions JsonProbeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class PackageManifestSnapshot
    {
        public string? PackageId { get; init; }
        public string? ValidationStatus { get; init; }
        public List<string>? BlockingReasons { get; init; }
        public List<string>? Warnings { get; init; }
        public List<string>? NextManualSteps { get; init; }
    }

    private sealed class PackageValidationSnapshot
    {
        public string? ValidationStatus { get; init; }
        public List<string>? BlockingReasons { get; init; }
        public List<string>? Warnings { get; init; }
        public List<string>? NextManualSteps { get; init; }
    }
}
