using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ItemSpritePreviewPipeline;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.ClientIdentity;
using RollblackLegacy.Admin.Application.DependencyInjection;
using RollblackLegacy.Admin.Contracts.ClientIdentity;
using RollblackLegacy.Admin.Infrastructure.DependencyInjection;

var options = PipelineOptions.Parse(args);
if (!options.Mode.Equals("audit", StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Phase 1 only supports --mode audit.");
}

var repoRoot = RepositoryRootResolver.Resolve(AppContext.BaseDirectory);
var paths = SpritePreviewPaths.Resolve(repoRoot);
var pathsConfig = RepositoryPaths.FromRepoRoot(repoRoot);

var settings = new HostApplicationBuilderSettings
{
    ContentRootPath = repoRoot,
    EnvironmentName = Environments.Development
};

var builder = Host.CreateApplicationBuilder(settings);
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(pathsConfig.AdminApiConfigDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.example.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.vps.example.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: false);

builder.Services.AddAdminApplication();
builder.Services.AddAdminInfrastructure(builder.Configuration);

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var identityService = scope.ServiceProvider.GetRequiredService<IClientItemIdentityReadService>();
var itemIds = ClientItemIdentityIdParser.Parse(options.RawIds);
var identityResults = await identityService.CheckAsync(new ClientItemIdentityCheckRequest(itemIds), CancellationToken.None);

var auditRows = SpritePreviewAuditRunner.BuildRows(paths, identityResults);
var appearanceProbe = BuildAppearanceProbe(paths, identityResults.FirstOrDefault()?.AppearancesD2oPath);

var report = SpritePreviewAuditRunner.WriteMarkdown(
    DateTimeOffset.UtcNow,
    paths,
    auditRows,
    appearanceProbe);

var outputDirectory = Path.IsPathRooted(options.OutputDirectory)
    ? options.OutputDirectory
    : Path.GetFullPath(Path.Combine(repoRoot, options.OutputDirectory));
Directory.CreateDirectory(outputDirectory);

var auditReportPath = Path.Combine(outputDirectory, "audit-report.md");
await File.WriteAllTextAsync(auditReportPath, report, Encoding.UTF8);
Console.WriteLine($"Audit report: {auditReportPath}");

if (!string.IsNullOrWhiteSpace(options.DocsReportPath))
{
    var docsPath = Path.IsPathRooted(options.DocsReportPath)
        ? options.DocsReportPath
        : Path.GetFullPath(Path.Combine(repoRoot, options.DocsReportPath));
    Directory.CreateDirectory(Path.GetDirectoryName(docsPath)!);

    var docsBody = new StringBuilder();
    docsBody.AppendLine("# Item Sprite Preview — Phase 1 Report");
    docsBody.AppendLine();
    docsBody.AppendLine("Estado: `DONE / PARTIAL` — auditoría y rutas validadas; extracción D2P pendiente Phase 2.");
    docsBody.AppendLine();
    docsBody.AppendLine($"Última generación: `{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss 'UTC'}`");
    docsBody.AppendLine();
    docsBody.AppendLine("Artefacto temporal: `Infrastructure/temporal-artifacts/item-sprite-preview-audit/audit-report.md`");
    docsBody.AppendLine();
    docsBody.AppendLine("## Tabla de casos");
    docsBody.AppendLine();
    docsBody.AppendLine(SpritePreviewAuditRunner.WriteDocsTable(auditRows));
    docsBody.AppendLine();
    docsBody.AppendLine("## Aparición 458 (control)");
    docsBody.AppendLine();
    docsBody.AppendLine($"- Hipótesis: `{appearanceProbe.Hypothesis}`");
    docsBody.AppendLine($"- Exists in Appearances.d2o (from identity layer): `{appearanceProbe.ExistsInAppearancesD2o}`");
    docsBody.AppendLine($"- Curated PNG: `{appearanceProbe.CuratedPath ?? "(missing)"}`");
    docsBody.AppendLine($"- Notas: {appearanceProbe.Notes}");
    docsBody.AppendLine();
    docsBody.AppendLine("Ver informe completo en temporal-artifacts y [sprite-preview-pipeline-phase1.md](./sprite-preview-pipeline-phase1.md).");

    await File.WriteAllTextAsync(docsPath, docsBody.ToString(), Encoding.UTF8);
    Console.WriteLine($"Docs report: {docsPath}");
}

return 0;

static AppearanceProbeResult BuildAppearanceProbe(SpritePreviewPaths paths, string? appearancesD2oPath)
{
    const int appearanceId = 458;
    var curatedPath = Path.Combine(paths.ByAppearanceDirectory, $"{appearanceId}.png");
    var existsCurated = File.Exists(curatedPath);
    var d2oPresent = File.Exists(appearancesD2oPath ?? paths.AppearancesD2oPath);

    return new AppearanceProbeResult(
        appearanceId,
        "Sombrero Jalato (no verificada en sunshine.items)",
        null,
        existsCurated ? curatedPath : null,
        d2oPresent
            ? "Phase 1 no indexa Appearances.d2o por id; ver items-client-appearance-mapping-audit.md. No afirmar mapping sin item de prueba en DB."
            : "Appearances.d2o no disponible en este workspace.");
}

internal sealed record PipelineOptions(string Mode, string RawIds, string OutputDirectory, string? DocsReportPath)
{
    public static PipelineOptions Parse(string[] args)
    {
        var mode = "audit";
        var rawIds = "7754,39,12617";
        var output = "Infrastructure/temporal-artifacts/item-sprite-preview-audit";
        string? docsReport = "docs/admin-tools/sprite-preview/item-sprite-preview-phase1-report.md";

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--mode" when index + 1 < args.Length:
                    mode = args[++index];
                    break;
                case "--items" when index + 1 < args.Length:
                    rawIds = args[++index];
                    break;
                case "--output" when index + 1 < args.Length:
                    output = args[++index];
                    break;
                case "--docs-report" when index + 1 < args.Length:
                    docsReport = args[++index];
                    break;
                case "--no-docs-report":
                    docsReport = null;
                    break;
            }
        }

        return new PipelineOptions(mode, rawIds, output, docsReport);
    }
}

internal static class RepositoryRootResolver
{
    public static string Resolve(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var hasAdmin = Directory.Exists(Path.Combine(directory.FullName, "Angular-tools", "Admin"));
            var hasDocs = Directory.Exists(Path.Combine(directory.FullName, "docs"));
            if (hasAdmin && hasDocs)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se pudo resolver la raiz del repo oficial desde ItemSpritePreviewPipeline.");
    }
}

internal sealed record RepositoryPaths(string RepoRoot, string AdminApiConfigDirectory)
{
    public static RepositoryPaths FromRepoRoot(string repoRoot) =>
        new(repoRoot, Path.Combine(repoRoot, "Angular-tools", "Admin", "RollblackLegacy.Admin.Api"));
}
