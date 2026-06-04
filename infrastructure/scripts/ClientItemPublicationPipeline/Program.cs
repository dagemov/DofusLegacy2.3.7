using System.Text;
using System.Text.Json;
using ClientItemPublicationPipeline;
using ClientItemPublicationPipeline.D2o;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.DependencyInjection;
using RollblackLegacy.Admin.Contracts.Items;
using RollblackLegacy.Admin.Infrastructure.DependencyInjection;

var options = PublicationPipelineOptions.Parse(args);
var repoRoot = RepositoryRootResolver.Resolve(AppContext.BaseDirectory);
var outputDirectory = ResolveOutputDirectory(repoRoot, options.OutputDirectory);
var sourceItems = Path.Combine(repoRoot, "Client2.3.7", "data", "common", "Items.d2o");

if (!File.Exists(sourceItems))
{
    throw new FileNotFoundException($"Items.d2o no encontrado: {sourceItems}");
}

return options.Mode.ToLowerInvariant() switch
{
    "dry-run" => await RunDryRunAsync(repoRoot, options, outputDirectory),
    "d2o-inspect-class" => RunD2oInspectClass(sourceItems, outputDirectory, options.D2oClassName),
    "d2o-roundtrip" => RunD2oRoundTrip(sourceItems, outputDirectory),
    "d2o-clone-item" => RunD2oCloneItem(sourceItems, outputDirectory, options),
    _ => throw new ArgumentException($"Modo no soportado: {options.Mode}")
};

static int RunD2oInspectClass(string sourceItems, string outputDirectory, string? focusClass)
{
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
    Console.WriteLine($"nameId={result.NameId} descriptionId={result.DescriptionId} (i18n pendiente)");
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

static string ResolveOutputDirectory(string repoRoot, string output) =>
    Path.IsPathRooted(output)
        ? output
        : Path.GetFullPath(Path.Combine(repoRoot, output));
