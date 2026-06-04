using System.Text;
using System.Text.Json;
using ClientItemPublicationPipeline;
using ClientItemPublicationPipeline.D2i;
using ClientItemPublicationPipeline.D2o;
using ClientItemPublicationPipeline.Package;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.DependencyInjection;
using RollblackLegacy.Admin.Contracts.Items;
using RollblackLegacy.Admin.Infrastructure.DependencyInjection;

var options = PublicationPipelineOptions.Parse(args);
var repoRoot = RepositoryRootResolver.Resolve(AppContext.BaseDirectory);
var paths = RepositoryPaths.FromRepoRoot(repoRoot);
var outputDirectory = ResolveOutputDirectory(repoRoot, options.OutputDirectory);

return options.Mode.ToLowerInvariant() switch
{
    "dry-run" => await RunDryRunAsync(repoRoot, options, outputDirectory),
    "d2o-inspect-class" => RunD2oInspectClass(paths.ClientItemsD2oPath, outputDirectory, options.D2oClassName),
    "d2o-roundtrip" => RunD2oRoundTrip(paths.ClientItemsD2oPath, outputDirectory),
    "d2o-clone-item" => RunD2oCloneItem(paths.ClientItemsD2oPath, outputDirectory, options),
    "d2i-inspect" => RunD2iInspect(paths, outputDirectory),
    "d2i-roundtrip" => RunD2iRoundTrip(paths, outputDirectory),
    "d2i-append-text" => RunD2iAppendText(paths, outputDirectory, options),
    "stage-item-publication" => RunStageItemPublication(repoRoot, paths, outputDirectory, options),
    "validate-publication-package" => RunValidatePublicationPackage(repoRoot, paths, options),
    "apply-package-to-sandbox" => RunApplyPackageToSandbox(repoRoot, options),
    "validate-sandbox-client" => RunValidateSandboxClient(repoRoot, options),
    "apply-package-to-real-client" => RunApplyPackageToRealClient(repoRoot, options),
    "validate-real-client" => RunValidateRealClient(repoRoot, options),
    "item-skin-catalog-dry-run" => RunItemSkinCatalogDryRun(repoRoot, options),
    "item-skin-catalog-export-curated" => RunItemSkinCatalogExportCurated(repoRoot, options),
    _ => throw new ArgumentException($"Modo no soportado: {options.Mode}")
};

static int RunD2iInspect(RepositoryPaths paths, string outputDirectory)
{
    EnsureI18nSources(paths);
    var publisher = new D2iStagingPublisher();
    var result = publisher.Inspect(paths.ClientI18nEsPath, paths.ClientI18nEnPath, outputDirectory);
    Console.WriteLine($"ES entries: {result.Es.IndexCount} (max id {result.Es.MaxTextId})");
    Console.WriteLine($"EN entries: {result.En.IndexCount} (max id {result.En.MaxTextId})");
    Console.WriteLine($"Report: {result.MarkdownPath}");
    return 0;
}

static int RunD2iRoundTrip(RepositoryPaths paths, string outputDirectory)
{
    EnsureI18nSources(paths);
    var publisher = new D2iStagingPublisher();
    var result = publisher.RoundTrip(paths.ClientI18nEsPath, paths.ClientI18nEnPath, outputDirectory);
    Console.WriteLine($"ES count: {result.BeforeEsCount} -> {result.AfterEsCount}");
    Console.WriteLine($"EN count: {result.BeforeEnCount} -> {result.AfterEnCount}");
    Console.WriteLine($"Success: {result.Success}");
    Console.WriteLine($"Report: {result.ReportPath}");
    return result.Success ? 0 : 1;
}

static int RunD2iAppendText(RepositoryPaths paths, string outputDirectory, PublicationPipelineOptions options)
{
    EnsureI18nSources(paths);
    var publisher = new D2iStagingPublisher();
    var result = publisher.AppendText(
        paths.ClientI18nEsPath,
        paths.ClientI18nEnPath,
        outputDirectory,
        options.EsName,
        options.EsDescription,
        options.EnName,
        options.EnDescription);

    Console.WriteLine($"NameId: {result.NameId}");
    Console.WriteLine($"DescriptionId: {result.DescriptionId}");
    Console.WriteLine($"Verified: {result.Verified}");
    Console.WriteLine($"ES: {result.ResolvedEsName}");
    Console.WriteLine($"EN: {result.ResolvedEnName}");
    Console.WriteLine($"JSON: {result.JsonPath}");
    Console.WriteLine($"Markdown: {result.MarkdownPath}");

    if (!options.StagePublicationPackage)
    {
        return result.Verified ? 0 : 1;
    }

    var packageDir = Path.Combine(
        paths.RepoRoot,
        "Infrastructure",
        "staging-client",
        "publication-package-phase3c",
        options.TargetItemId.ToString());

    var package = publisher.TryStagePublicationPackage(
        paths.RepoRoot,
        packageDir,
        options.SourceItemId,
        options.TargetItemId,
        result,
        options.CloneTypeId,
        options.CloneIconId,
        options.CloneAppearanceId);

    if (package is null)
    {
        return 1;
    }

    Console.WriteLine($"Package: {package.PackageDirectory}");
    Console.WriteLine($"Manifest: {package.JsonPath}");
    return result.Verified ? 0 : 1;
}

static int RunStageItemPublication(
    string repoRoot,
    RepositoryPaths paths,
    string outputDirectory,
    PublicationPipelineOptions options)
{
    EnsureI18nSources(paths);
    EnsureItemsSource(paths);
    var publisher = new D2iStagingPublisher();
    var i18nDir = outputDirectory;
    var append = publisher.AppendText(
        paths.ClientI18nEsPath,
        paths.ClientI18nEnPath,
        i18nDir,
        options.EsName,
        options.EsDescription,
        options.EnName,
        options.EnDescription);

    if (!append.Verified)
    {
        return 1;
    }

    var packageDir = string.IsNullOrWhiteSpace(outputDirectory)
        ? Path.Combine(repoRoot, "Infrastructure", "staging-client", "publication-package-phase3c", options.TargetItemId.ToString())
        : outputDirectory;

    var package = publisher.TryStagePublicationPackage(
        repoRoot,
        packageDir,
        options.SourceItemId,
        options.TargetItemId,
        append,
        options.CloneTypeId,
        options.CloneIconId,
        options.CloneAppearanceId);

    Console.WriteLine($"Package: {package?.PackageDirectory}");
    Console.WriteLine($"nameId={package?.NameId} descriptionId={package?.DescriptionId}");
    Console.WriteLine($"Validation: {package?.ValidationStatus} (ok={package?.ValidationPassed})");
    return package is not null && append.Verified && package.ValidationPassed ? 0 : 1;
}

static int RunValidatePublicationPackage(string repoRoot, RepositoryPaths paths, PublicationPipelineOptions options)
{
    var packageDirectory = string.IsNullOrWhiteSpace(options.PackageDirectory)
        ? Path.Combine(repoRoot, "Infrastructure", "staging-client", "publication-package-phase3c", options.TargetItemId.ToString())
        : ResolveOutputDirectory(repoRoot, options.PackageDirectory!);

    if (!Directory.Exists(packageDirectory))
    {
        throw new DirectoryNotFoundException($"Paquete no encontrado: {packageDirectory}");
    }

    var clientRoot = Path.Combine(repoRoot, "Client2.3.7");
    var adminByIcon = Path.Combine(
        repoRoot,
        "Angular-tools",
        "Admin",
        "RollblackLegacy.Admin.Angular",
        "src",
        "assets",
        "item-previews",
        "by-icon");

    var validator = new PublicationPackageValidator();
    var result = validator.Validate(
        new PublicationPackageValidationRequest(
            packageDirectory,
            repoRoot,
            clientRoot,
            adminByIcon,
            Path.Combine(clientRoot, "data", "common", "ItemTypes.d2o"),
            options.TargetItemId > 0 ? options.TargetItemId : options.ItemId,
            options.CloneTypeId,
            null,
            null,
            options.CloneAppearanceId));

    PublicationPackageChecksumWriter.WriteChecksumsFile(packageDirectory, result.Checksums);

    PublicationPackageManifestDocument? manifest = null;
    var manifestPath = Path.Combine(packageDirectory, PublicationPackagePaths.ManifestJson);
    if (File.Exists(manifestPath))
    {
        try
        {
            manifest = System.Text.Json.JsonSerializer.Deserialize<PublicationPackageManifestDocument>(
                File.ReadAllText(manifestPath),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            manifest = null;
        }
    }

    manifest ??= new PublicationPackageManifestDocument
    {
        PackageId = $"staging-publication-{options.TargetItemId}-revalidate",
        CreatedAt = DateTimeOffset.UtcNow,
        TargetItemId = options.TargetItemId > 0 ? options.TargetItemId : options.ItemId,
        IsProductionPackage = false
    };

    validator.WriteReports(packageDirectory, result, manifest with
    {
        ValidationStatus = result.ValidationStatus,
        BlockingReasons = result.BlockingReasons,
        Warnings = result.Warnings,
        NextManualSteps = result.NextManualSteps,
        Checksums = result.Checksums,
        GeneratedFiles =
        [
            PublicationPackagePaths.ItemsRelative,
            PublicationPackagePaths.I18nEsRelative,
            PublicationPackagePaths.I18nEnRelative
        ]
    });

    Console.WriteLine($"Package: {packageDirectory}");
    Console.WriteLine($"Valid: {result.IsValid}");
    Console.WriteLine($"Status: {result.ValidationStatus}");
    foreach (var reason in result.BlockingReasons)
    {
        Console.WriteLine($"BLOCK: {reason}");
    }

    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"WARN: {warning}");
    }

    return result.IsValid ? 0 : 1;
}

static int RunD2oInspectClass(string sourceItems, string outputDirectory, string? focusClass)
{
    EnsureItemsSourcePath(sourceItems);
    var publisher = new D2oStagingPublisher();
    var result = publisher.InspectClass(sourceItems, outputDirectory, focusClass);
    Console.WriteLine($"Index entries: {result.IndexCount}");
    Console.WriteLine($"Class definitions: {result.ClassCount}");
    Console.WriteLine($"Markdown: {result.MarkdownPath}");
    Console.WriteLine($"JSON: {result.JsonPath}");
    return 0;
}

static int RunD2oRoundTrip(string sourceItems, string outputDirectory)
{
    EnsureItemsSourcePath(sourceItems);
    var publisher = new D2oStagingPublisher();
    var result = publisher.RoundTrip(sourceItems, outputDirectory);
    Console.WriteLine($"Index before: {result.BeforeCount}");
    Console.WriteLine($"Index after: {result.AfterCount}");
    Console.WriteLine($"Item 7754 readable: {result.Item7754Readable}");
    Console.WriteLine($"Report: {result.ReportPath}");
    return result.BeforeCount == result.AfterCount && result.Item7754Readable ? 0 : 1;
}

static int RunD2oCloneItem(string sourceItems, string outputDirectory, PublicationPipelineOptions options)
{
    EnsureItemsSourcePath(sourceItems);
    var publisher = new D2oStagingPublisher();
    var result = publisher.CloneItem(
        sourceItems,
        outputDirectory,
        options.SourceItemId,
        options.TargetItemId,
        options.CloneTypeId,
        options.CloneIconId,
        options.CloneAppearanceId);

    Console.WriteLine($"Clone {result.SourceItemId} -> {result.TargetItemId}");
    Console.WriteLine($"Target exists: {result.TargetExists}");
    Console.WriteLine($"typeId={result.TypeId} iconId={result.IconId} appearanceId={result.AppearanceId}");
    Console.WriteLine($"nameId={result.NameId} descriptionId={result.DescriptionId}");
    Console.WriteLine($"Staging: {result.StagingItemsPath}");
    return result.TargetExists ? 0 : 1;
}

static async Task<int> RunDryRunAsync(string repoRoot, PublicationPipelineOptions options, string outputDirectory)
{
    var paths = RepositoryPaths.FromRepoRoot(repoRoot);
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        ContentRootPath = repoRoot,
        EnvironmentName = Environments.Development
    });
    builder.Configuration.Sources.Clear();
    builder.Configuration
        .SetBasePath(paths.AdminApiConfigDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.Development.example.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.Development.vps.example.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: false);

    builder.Services.AddAdminApplication();
    builder.Services.AddAdminInfrastructure(builder.Configuration);

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var manifestService = scope.ServiceProvider.GetRequiredService<IItemPublicationManifestService>();
    var manifest = await manifestService.GetManifestAsync(options.ItemId, CancellationToken.None);

    Directory.CreateDirectory(outputDirectory);
    var jsonPath = Path.Combine(outputDirectory, "publication-manifest.json");
    var mdPath = Path.Combine(outputDirectory, "publication-manifest.md");

    var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(jsonPath, json, Encoding.UTF8);
    await File.WriteAllTextAsync(mdPath, PublicationManifestMarkdownWriter.Write(manifest), Encoding.UTF8);

    Console.WriteLine($"ItemId: {manifest.DbItemId}");
    Console.WriteLine($"PrimaryState: {manifest.PrimaryState}");
    Console.WriteLine($"ClientKnown: {manifest.ClientKnown}");
    Console.WriteLine($"CanPublishAutomatically: {manifest.CanPublishAutomatically}");
    Console.WriteLine($"JSON: {jsonPath}");
    Console.WriteLine($"Markdown: {mdPath}");
    return 0;
}

static void EnsureI18nSources(RepositoryPaths paths)
{
    if (!File.Exists(paths.ClientI18nEsPath) || !File.Exists(paths.ClientI18nEnPath))
    {
        throw new FileNotFoundException("Archivos i18n del cliente no encontrados en Client2.3.7/data/i18n/");
    }
}

static void EnsureItemsSource(RepositoryPaths paths) => EnsureItemsSourcePath(paths.ClientItemsD2oPath);

static void EnsureItemsSourcePath(string path)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException($"Items.d2o no encontrado: {path}");
    }
}

static int RunApplyPackageToSandbox(string repoRoot, PublicationPipelineOptions options)
{
    var packageDir = string.IsNullOrWhiteSpace(options.PackageDirectory)
        ? Path.Combine(repoRoot, "Infrastructure", "staging-client", "publication-package-phase3c", options.TargetItemId.ToString())
        : ResolveOutputDirectory(repoRoot, options.PackageDirectory!);

    var sandboxDir = string.IsNullOrWhiteSpace(options.SandboxDirectory)
        ? Path.Combine(repoRoot, "Infrastructure", "staging-client", "client-patch-sandbox", options.TargetItemId.ToString())
        : ResolveOutputDirectory(repoRoot, options.SandboxDirectory!);

    if (!Directory.Exists(packageDir))
    {
        throw new DirectoryNotFoundException($"Paquete no encontrado: {packageDir}");
    }

    var publisher = new ClientPatchSandboxPublisher();
    var result = publisher.ApplyPackageToSandbox(repoRoot, packageDir, sandboxDir, options.TargetItemId);
    Console.WriteLine($"Sandbox: {result.SandboxDirectory}");
    Console.WriteLine($"Manifest: {result.ManifestPath}");
    return 0;
}

static int RunValidateSandboxClient(string repoRoot, PublicationPipelineOptions options)
{
    var sandboxDir = string.IsNullOrWhiteSpace(options.SandboxDirectory)
        ? Path.Combine(repoRoot, "Infrastructure", "staging-client", "client-patch-sandbox", options.TargetItemId.ToString())
        : ResolveOutputDirectory(repoRoot, options.SandboxDirectory!);

    if (!Directory.Exists(sandboxDir))
    {
        throw new DirectoryNotFoundException($"Sandbox no encontrado: {sandboxDir}");
    }

    var publisher = new ClientPatchSandboxPublisher();
    var result = publisher.ValidateSandbox(repoRoot, sandboxDir, options.TargetItemId, options.CloneIconId);
    Console.WriteLine($"Sandbox: {sandboxDir}");
    Console.WriteLine($"Valid: {result.IsValid}");
    Console.WriteLine($"Status: {result.ValidationStatus}");
    foreach (var reason in result.BlockingReasons)
    {
        Console.WriteLine($"BLOCK: {reason}");
    }

    return result.IsValid ? 0 : 1;
}

static string ResolveOutputDirectory(string repoRoot, string output) =>
    Path.IsPathRooted(output)
        ? output
        : Path.GetFullPath(Path.Combine(repoRoot, output));

static string ResolveClientRoot(string repoRoot, PublicationPipelineOptions options) =>
    string.IsNullOrWhiteSpace(options.ClientDirectory)
        ? Path.Combine(repoRoot, "Client2.3.7")
        : ResolveOutputDirectory(repoRoot, options.ClientDirectory!);

static int RunApplyPackageToRealClient(string repoRoot, PublicationPipelineOptions options)
{
    var packageDir = string.IsNullOrWhiteSpace(options.PackageDirectory)
        ? Path.Combine(repoRoot, "Infrastructure", "staging-client", "publication-package-phase3c", options.TargetItemId.ToString())
        : ResolveOutputDirectory(repoRoot, options.PackageDirectory!);

    var clientRoot = ResolveClientRoot(repoRoot, options);
    if (!Directory.Exists(packageDir))
    {
        throw new DirectoryNotFoundException($"Paquete no encontrado: {packageDir}");
    }

    var publisher = new ClientPatchRealPublisher();
    var result = publisher.ApplyPackageToRealClient(repoRoot, packageDir, clientRoot, options.TargetItemId);
    Console.WriteLine($"Client: {result.ClientRoot}");
    Console.WriteLine($"Backup: {result.BackupDirectory}");
    Console.WriteLine($"Manifest: {result.ManifestPath}");
    return 0;
}

static int RunValidateRealClient(string repoRoot, PublicationPipelineOptions options)
{
    var clientRoot = ResolveClientRoot(repoRoot, options);
    var itemId = options.TargetItemId > 0 ? options.TargetItemId : options.ItemId;
    var publisher = new ClientPatchRealPublisher();
    var result = publisher.ValidateRealClient(
        repoRoot,
        clientRoot,
        itemId,
        options.CloneIconId);

    Console.WriteLine($"Client: {clientRoot}");
    Console.WriteLine($"Valid: {result.IsValid}");
    Console.WriteLine($"Status: {result.ValidationStatus}");
    foreach (var reason in result.BlockingReasons)
    {
        Console.WriteLine($"BLOCK: {reason}");
    }

    return result.IsValid ? 0 : 1;
}

static int RunItemSkinCatalogDryRun(string repoRoot, PublicationPipelineOptions options)
{
    var outputDirectory = ResolveOutputDirectory(repoRoot, options.OutputDirectory);
    var clientRoot = ResolveClientRoot(repoRoot, options);
    var excludeWeapons = WeaponTypeFilter.ExcludeWeapons(options.ExcludeTypes);
    var runner = new ItemSkinCatalogDryRunner();
    var result = runner.Run(repoRoot, outputDirectory, clientRoot, excludeWeapons);
    Console.WriteLine($"Catalog entries: {result.Summary.CatalogEntries}");
    Console.WriteLine($"Skipped weapons: {result.Summary.SkippedWeapons}");
    Console.WriteLine($"With icon preview: {result.Summary.WithIconPreview}");
    Console.WriteLine($"JSON: {result.JsonPath}");
    Console.WriteLine($"Markdown: {result.MarkdownPath}");
    Console.WriteLine($"Gallery: {result.GalleryHtmlPath}");
    return 0;
}

static int RunItemSkinCatalogExportCurated(string repoRoot, PublicationPipelineOptions options)
{
    if (string.IsNullOrWhiteSpace(options.Category))
    {
        throw new ArgumentException("--category es obligatorio para item-skin-catalog-export-curated.");
    }

    var outputDirectory = ResolveOutputDirectory(repoRoot, options.OutputDirectory);
    var clientRoot = ResolveClientRoot(repoRoot, options);
    var paths = ClientSkinCatalogPaths.Resolve(repoRoot, clientRoot);
    var builder = new ItemSkinCatalogBuilder();
    var catalog = builder.Build(paths, outputDirectory, excludeWeapons: true);
    var exporter = new ItemSkinCatalogExporter();
    var export = exporter.ExportCurated(
        paths,
        catalog,
        options.Category!,
        options.CatalogLimit,
        options.CatalogDryRun || !options.ApproveCuratedCopy,
        options.ApproveCuratedCopy);

    Console.WriteLine($"Category: {export.Category}");
    Console.WriteLine($"Planned: {export.PlannedCopies}");
    Console.WriteLine($"Copied: {export.Copied}");
    Console.WriteLine($"DryRun: {export.DryRun}");
    foreach (var message in export.Messages.Take(20))
    {
        Console.WriteLine(message);
    }

    return 0;
}
