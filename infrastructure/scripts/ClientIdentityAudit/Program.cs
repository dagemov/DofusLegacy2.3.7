using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;
using RollblackLegacy.Admin.Application.DependencyInjection;
using RollblackLegacy.Admin.Contracts.ClientIdentity;
using RollblackLegacy.Admin.Infrastructure.DependencyInjection;

var options = AuditOptions.Parse(args);
var repoRoot = RepositoryRootResolver.Resolve(AppContext.BaseDirectory);
var paths = RepositoryPaths.FromRepoRoot(repoRoot);

var settings = new HostApplicationBuilderSettings
{
    ContentRootPath = repoRoot,
    EnvironmentName = Environments.Development
};

var builder = Host.CreateApplicationBuilder(settings);
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
var auditService = scope.ServiceProvider.GetRequiredService<IClientItemIdentityReadService>();
var results = await auditService.CheckAsync(new ClientItemIdentityCheckRequest(options.ItemIds));

var report = MarkdownReportWriter.Write(new AuditReport(
    GeneratedAtUtc: DateTimeOffset.UtcNow,
    RepoRoot: repoRoot,
    Items: results));

if (!string.IsNullOrWhiteSpace(options.OutputPath))
{
    var outputPath = Path.IsPathRooted(options.OutputPath)
        ? options.OutputPath
        : Path.GetFullPath(Path.Combine(repoRoot, options.OutputPath));
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    await File.WriteAllTextAsync(outputPath, report, Encoding.UTF8);
    Console.WriteLine($"Reporte escrito en: {outputPath}");
}
else
{
    Console.WriteLine(report);
}

return 0;

internal sealed record AuditOptions(IReadOnlyList<int> ItemIds, string? OutputPath)
{
    public static AuditOptions Parse(string[] args)
    {
        var items = new List<int>();
        string? output = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--items" when index + 1 < args.Length:
                    items.AddRange(args[++index]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(static value => int.Parse(value, CultureInfo.InvariantCulture)));
                    break;
                case "--output" when index + 1 < args.Length:
                    output = args[++index];
                    break;
            }
        }

        if (items.Count == 0)
        {
            items.AddRange([7754, 12616, 12617, 39]);
        }

        return new AuditOptions(items.Distinct().ToArray(), output);
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

        throw new DirectoryNotFoundException("No se pudo resolver la raiz del repo oficial desde la tool.");
    }
}

internal sealed record RepositoryPaths(string RepoRoot, string AdminApiConfigDirectory)
{
    public static RepositoryPaths FromRepoRoot(string repoRoot)
    {
        return new RepositoryPaths(
            repoRoot,
            Path.Combine(repoRoot, "Angular-tools", "Admin", "RollblackLegacy.Admin.Api"));
    }
}

internal sealed record AuditReport(
    DateTimeOffset GeneratedAtUtc,
    string RepoRoot,
    IReadOnlyList<ClientItemIdentityCheckResultDto> Items);

internal static class MarkdownReportWriter
{
    public static string Write(AuditReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Client Identity Item Check Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: `{report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}`");
        builder.AppendLine();
        builder.AppendLine("## Inputs");
        builder.AppendLine();
        builder.AppendLine($"- Repo: `{report.RepoRoot}`");

        var firstItem = report.Items.FirstOrDefault();
        if (firstItem is not null)
        {
            builder.AppendLine($"- Items.d2o: `{firstItem.ItemsD2oPath}`");
            builder.AppendLine($"- ItemTypes.d2o: `{firstItem.ItemTypesD2oPath}`");
            builder.AppendLine($"- ItemSets.d2o: `{firstItem.ItemSetsD2oPath}`");
            builder.AppendLine($"- Appearances.d2o: `{firstItem.AppearancesD2oPath}`");
            builder.AppendLine($"- i18n_es.d2i: `{firstItem.I18nEsPath}`");
            builder.AppendLine($"- i18n_en.d2i: `{firstItem.I18nEnPath}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| ItemId | DB Name | Client | Statuses | Preview |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (var item in report.Items)
        {
            builder.AppendLine($"| `{item.ItemId}` | {Escape(item.DbName)} | {(item.ClientKnown ? "KNOWN" : "UNKNOWN")} | {Escape(string.Join(", ", item.Status.Statuses))} | {Escape(item.PreviewPath is null ? "missing" : Path.GetFileName(item.PreviewPath))} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Detailed results");
        builder.AppendLine();

        foreach (var item in report.Items)
        {
            builder.AppendLine($"### Item `{item.ItemId}`");
            builder.AppendLine();
            builder.AppendLine($"- DB Name: `{NormalizeInline(item.DbName)}`");
            builder.AppendLine($"- Client known: `{item.ClientKnown}`");
            builder.AppendLine($"- Primary status: `{item.Status.PrimaryStatus}`");
            builder.AppendLine($"- Statuses: `{string.Join(", ", item.Status.Statuses)}`");
            builder.AppendLine($"- Warnings: `{NormalizeInline(string.Join(" | ", item.Status.Warnings))}`");
            builder.AppendLine($"- Recommended action: `{NormalizeInline(item.Status.RecommendedAction)}`");
            builder.AppendLine($"- Preview path: `{item.PreviewPath ?? "(missing)"}`");
            builder.AppendLine($"- DB DescriptionId / Client DescriptionId: `{item.DbDescriptionId?.ToString() ?? "(missing)"} / {item.ClientDescriptionId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- Client NameId: `{item.ClientNameId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- DB Description ES: `{NormalizeInline(item.DescriptionEs.Text)}`");
            builder.AppendLine($"- DB Description EN: `{NormalizeInline(item.DescriptionEn.Text)}`");
            builder.AppendLine($"- Client Name ES: `{NormalizeInline(item.ClientNameEs.Text)}`");
            builder.AppendLine($"- Client Name EN: `{NormalizeInline(item.ClientNameEn.Text)}`");
            builder.AppendLine($"- DB TypeId / Client TypeId: `{item.DbTypeId?.ToString() ?? "(missing)"} / {item.ClientTypeId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- Client Type ES / EN: `{NormalizeInline(item.ClientTypeNameEs)}` / `{NormalizeInline(item.ClientTypeNameEn)}`");
            builder.AppendLine($"- DB SetId / Client SetId: `{item.DbSetId?.ToString() ?? "(missing)"} / {item.ClientSetId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- Client Set ES / EN: `{NormalizeInline(item.ClientSetNameEs)}` / `{NormalizeInline(item.ClientSetNameEn)}`");
            builder.AppendLine($"- DB IconId / Client IconId: `{item.DbIconId?.ToString() ?? "(missing)"} / {item.ClientIconId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- DB AppearanceId / Client AppearanceId: `{item.DbAppearanceId?.ToString() ?? "(missing)"} / {item.ClientAppearanceId?.ToString() ?? "(missing)"}`");
            builder.AppendLine($"- Appearance known: `{item.Appearance.Exists?.ToString() ?? "(n/a)"}`");
            builder.AppendLine();
        }

        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine("- `CLIENT_KNOWN`: el `ItemId` existe en `Items.d2o`.");
        builder.AppendLine("- `CLIENT_UNKNOWN`: el `ItemId` no existe en `Items.d2o`.");
        builder.AppendLine("- `SAFE_EXISTING_TEMPLATE`: el cliente ya conoce el template actual.");
        builder.AppendLine("- `NEEDS_CLIENT_PATCH`: hace falta publicar template cliente o alinear metadata.");
        builder.AppendLine("- `I18N_MISSING_ES` / `I18N_MISSING_EN`: `DescriptionId` DB no resolvio en ese idioma.");
        builder.AppendLine("- `ICON_MISSING`: el item no trae `IconId` usable en DB.");
        builder.AppendLine("- `APPEARANCE_UNKNOWN`: `AppearanceId` > 0, pero no existe en `Appearances.d2o`.");
        builder.AppendLine("- `CLIENT_DATA_UNAVAILABLE`: la tool no pudo leer los metadata del cliente desde este entorno.");
        return builder.ToString();
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Replace("|", "\\|");

    private static string NormalizeInline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(missing)";
        }

        return value.Replace("`", "'").Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
