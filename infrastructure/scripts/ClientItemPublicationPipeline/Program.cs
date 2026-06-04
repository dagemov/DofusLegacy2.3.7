using System.Text;
using System.Text.Json;
using ClientItemPublicationPipeline;
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

if (!options.Mode.Equals("dry-run", StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException($"Modo no soportado: {options.Mode}. Usa --mode dry-run.");
}

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

static string ResolveOutputDirectory(string repoRoot, string output) =>
    Path.IsPathRooted(output)
        ? output
        : Path.GetFullPath(Path.Combine(repoRoot, output));
