using System.Text;
using ItemSpritePreviewPipeline;
using ItemSpritePreviewPipeline.D2p;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.ClientIdentity;
using RollblackLegacy.Admin.Application.DependencyInjection;
using RollblackLegacy.Admin.Contracts.ClientIdentity;
using RollblackLegacy.Admin.Infrastructure.DependencyInjection;

var options = PipelineOptions.Parse(args);
var repoRoot = RepositoryRootResolver.Resolve(AppContext.BaseDirectory);
var paths = SpritePreviewPaths.Resolve(repoRoot);
var outputDirectory = ResolveOutputDirectory(repoRoot, options.OutputDirectory);

var exitCode = options.Mode.ToLowerInvariant() switch
{
    "audit" => await RunIdentityAuditAsync(repoRoot, paths, options, outputDirectory),
    "d2p-audit" => RunD2pAudit(repoRoot, paths, options, outputDirectory),
    "extract-icon" => RunExtractIcon(repoRoot, paths, options, outputDirectory),
    _ => throw new ArgumentException($"Modo no soportado: {options.Mode}. Usa audit, d2p-audit o extract-icon.")
};

return exitCode;

async Task<int> RunIdentityAuditAsync(
    string repoRoot,
    SpritePreviewPaths paths,
    PipelineOptions options,
    string outputDirectory)
{
    var pathsConfig = RepositoryPaths.FromRepoRoot(repoRoot);
    var builder = CreateAdminHostBuilder(repoRoot, pathsConfig);
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

    Directory.CreateDirectory(outputDirectory);
    var auditReportPath = Path.Combine(outputDirectory, "audit-report.md");
    await File.WriteAllTextAsync(auditReportPath, report, Encoding.UTF8);
    Console.WriteLine($"Audit report: {auditReportPath}");

    if (!string.IsNullOrWhiteSpace(options.DocsReportPath))
    {
        await WritePhase1DocsReportAsync(repoRoot, options.DocsReportPath, auditRows, appearanceProbe);
    }

    return 0;
}

int RunD2pAudit(string repoRoot, SpritePreviewPaths paths, PipelineOptions options, string outputDirectory)
{
    Directory.CreateDirectory(outputDirectory);
    var packPaths = paths.ItemBitmapD2pPaths.Concat(paths.ItemVectorD2pPaths).ToArray();
    var packs = D2pPackAuditor.AuditPacks(packPaths);
    var probeIconId = options.IconId ?? 23012;
    var matches = D2pIconExtractor.FindMatches(paths.ItemBitmapD2pPaths, probeIconId);

    var report = D2pPackAuditor.WriteMarkdown(DateTimeOffset.UtcNow, repoRoot, packs, probeIconId, matches);
    var reportPath = Path.Combine(outputDirectory, "d2p-audit-report.md");
    File.WriteAllText(reportPath, report, Encoding.UTF8);
    Console.WriteLine($"D2P audit report: {reportPath}");
    Console.WriteLine($"IconId {probeIconId} matches: {matches.Count}");
    return 0;
}

int RunExtractIcon(string repoRoot, SpritePreviewPaths paths, PipelineOptions options, string outputDirectory)
{
    if (options.IconId is not > 0)
    {
        throw new ArgumentException("extract-icon requiere --icon-id positivo.");
    }

    var iconId = options.IconId.Value;
    var matches = D2pIconExtractor.FindMatches(paths.ItemBitmapD2pPaths, iconId);
    var copyPlan = CuratedIconCopyPlanner.Plan(paths, iconId, matches);

    if (options.DryRunCuratedCopy)
    {
        CuratedIconCopyPlanner.PrintDryRunConsole(copyPlan);
        Directory.CreateDirectory(outputDirectory);
        var dryRunPath = Path.Combine(outputDirectory, $"curated-copy-dry-run-{iconId}.md");
        File.WriteAllText(dryRunPath, CuratedIconCopyPlanner.WriteDryRunReport(copyPlan), Encoding.UTF8);
        Console.WriteLine($"Dry-run report: {dryRunPath}");
        return copyPlan.SourceFound && copyPlan.PngSignatureValid ? 0 : 1;
    }

    Directory.CreateDirectory(outputDirectory);
    var result = D2pIconExtractor.ExtractIcon(paths.ItemBitmapD2pPaths, iconId, outputDirectory);
    var reportPath = Path.Combine(outputDirectory, $"extract-icon-{iconId}.md");
    File.WriteAllText(reportPath, D2pIconExtractor.WriteExtractionMarkdown(result), Encoding.UTF8);

    Console.WriteLine(result.Message);
    Console.WriteLine($"Extraction report: {reportPath}");

    if (!options.ApproveCuratedCopy)
    {
        return result.Success ? 0 : 1;
    }

    if (!result.Success || string.IsNullOrWhiteSpace(result.OutputFilePath))
    {
        Console.Error.WriteLine("No se puede aprobar copia curada: extracción fallida.");
        return 1;
    }

    if (!CuratedIconCopyPlanner.CanApproveCopy(copyPlan, options.OverwriteCurated, out var approveError))
    {
        Console.Error.WriteLine(approveError);
        return 1;
    }

    Directory.CreateDirectory(paths.ByIconDirectory);
    File.Copy(result.OutputFilePath, copyPlan.TargetPath, overwrite: options.OverwriteCurated);
    Console.WriteLine($"Copiado a catálogo curado: {copyPlan.TargetPath}");
    return 0;
}

HostApplicationBuilder CreateAdminHostBuilder(string repoRoot, RepositoryPaths pathsConfig)
{
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
    return builder;
}

string ResolveOutputDirectory(string repoRoot, string output) =>
    Path.IsPathRooted(output)
        ? output
        : Path.GetFullPath(Path.Combine(repoRoot, output));

async Task WritePhase1DocsReportAsync(
    string repoRoot,
    string docsReportPath,
    IReadOnlyList<SpritePreviewAuditRow> auditRows,
    AppearanceProbeResult appearanceProbe)
{
    var docsPath = Path.IsPathRooted(docsReportPath)
        ? docsReportPath
        : Path.GetFullPath(Path.Combine(repoRoot, docsReportPath));
    Directory.CreateDirectory(Path.GetDirectoryName(docsPath)!);

    var docsBody = new StringBuilder();
    docsBody.AppendLine("# Item Sprite Preview — Phase 1 Report");
    docsBody.AppendLine();
    docsBody.AppendLine("Estado: `DONE` — ver [Phase 2 D2P](./sprite-preview-d2p-extractor-phase2.md) para extracción.");
    docsBody.AppendLine();
    docsBody.AppendLine($"Última generación identity audit: `{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss 'UTC'}`");
    docsBody.AppendLine();
    docsBody.AppendLine("## Tabla de casos");
    docsBody.AppendLine();
    docsBody.AppendLine(SpritePreviewAuditRunner.WriteDocsTable(auditRows));
    docsBody.AppendLine();
    docsBody.AppendLine("## Aparición 458 (control)");
    docsBody.AppendLine();
    docsBody.AppendLine($"- Hipótesis: `{appearanceProbe.Hypothesis}`");
    docsBody.AppendLine($"- Curated PNG: `{appearanceProbe.CuratedPath ?? "(missing)"}`");
    docsBody.AppendLine($"- Notas: {appearanceProbe.Notes}");

    await File.WriteAllTextAsync(docsPath, docsBody.ToString(), Encoding.UTF8);
    Console.WriteLine($"Docs report: {docsPath}");
}

AppearanceProbeResult BuildAppearanceProbe(SpritePreviewPaths paths, string? appearancesD2oPath)
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
            ? "Ver items-client-appearance-mapping-audit.md."
            : "Appearances.d2o no disponible en este workspace.");
}

internal sealed record PipelineOptions(
    string Mode,
    string RawIds,
    string OutputDirectory,
    string? DocsReportPath,
    int? IconId,
    bool DryRunCuratedCopy,
    bool ApproveCuratedCopy,
    bool OverwriteCurated)
{
    public static PipelineOptions Parse(string[] args)
    {
        var mode = "audit";
        var rawIds = "7754,39,12617";
        var output = "Infrastructure/temporal-artifacts/item-sprite-preview-audit";
        string? docsReport = null;
        int? iconId = null;
        var dryRunCuratedCopy = false;
        var approveCuratedCopy = false;
        var overwriteCurated = false;

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
                case "--icon-id" when index + 1 < args.Length:
                    iconId = int.Parse(args[++index]);
                    break;
                case "--dry-run-curated-copy":
                    dryRunCuratedCopy = true;
                    break;
                case "--approve-curated-copy":
                    approveCuratedCopy = true;
                    break;
                case "--overwrite-curated":
                    overwriteCurated = true;
                    break;
            }
        }

        if (dryRunCuratedCopy && approveCuratedCopy)
        {
            throw new ArgumentException("Usa solo uno: --dry-run-curated-copy o --approve-curated-copy.");
        }

        if (mode.Equals("audit", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(docsReport))
        {
            docsReport = "docs/admin-tools/sprite-preview/item-sprite-preview-phase1-report.md";
        }

        return new PipelineOptions(
            mode,
            rawIds,
            output,
            docsReport,
            iconId,
            dryRunCuratedCopy,
            approveCuratedCopy,
            overwriteCurated);
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
