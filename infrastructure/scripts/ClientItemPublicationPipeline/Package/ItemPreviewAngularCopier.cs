using System.Text;
using System.Text.Json;

namespace ClientItemPublicationPipeline.Package;

internal sealed record ItemPreviewCopyResult(
    int Planned,
    int Copied,
    int SkippedExisting,
    int SkippedLimit,
    int WeaponsCopied,
    string ManifestJsonPath,
    string ManifestMarkdownPath,
    string AssetsManifestPath,
    IReadOnlyList<string> Messages);

internal sealed class ItemPreviewAngularCopier
{
    private const int DefaultMaxCopy = 500;

    public ItemPreviewCopyResult CopyToAngular(
        string repoRoot,
        string sourceExportDirectory,
        bool approveCuratedCopy,
        bool overwriteCurated,
        int? maxCopy = null)
    {
        if (!approveCuratedCopy)
        {
            throw new InvalidOperationException(
                "Copia a Angular bloqueada. Pase --approve-curated-copy para confirmar.");
        }

        var catalogPath = Path.Combine(sourceExportDirectory, "catalog.json");
        if (!File.Exists(catalogPath))
        {
            throw new FileNotFoundException($"catalog.json no encontrado en: {sourceExportDirectory}");
        }

        var document = JsonSerializer.Deserialize<ItemPreviewCatalogDocument>(
            File.ReadAllText(catalogPath),
            CatalogJsonOptions);

        if (document is null || document.Extracted.Count == 0)
        {
            throw new InvalidOperationException("catalog.json vacio o ilegible.");
        }

        var paths = ClientSkinCatalogPaths.Resolve(repoRoot, Path.Combine(repoRoot, "Client2.3.7"));
        var pngSourceRoot = Path.Combine(sourceExportDirectory, "png", "by-category");
        var max = maxCopy ?? DefaultMaxCopy;
        var messages = new List<string>();
        var copied = 0;
        var skippedExisting = 0;
        var skippedLimit = 0;
        var weaponsCopied = 0;
        var manifestEntries = new List<ItemPreviewCuratedManifestEntry>();

        var ordered = document.Extracted
            .OrderByDescending(static e => string.Equals(e.Category, "dofus", StringComparison.OrdinalIgnoreCase))
            .ThenBy(static e => e.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static e => e.ItemId)
            .ToList();

        foreach (var entry in ordered)
        {
            if (IsBlockedWeaponCategory(entry.Category))
            {
                weaponsCopied++;
                messages.Add($"[blocked-weapon-category] {entry.Category} ItemId {entry.ItemId}");
                continue;
            }

            if (!ItemSkinCategoryMap.IsSupportedExportCategory(entry.Category))
            {
                messages.Add($"[skip-unsupported-category] {entry.Category}");
                continue;
            }

            if (copied >= max)
            {
                skippedLimit++;
                continue;
            }

            var sourceFile = Path.Combine(pngSourceRoot, entry.Category, $"{entry.IconId}.png");
            var targetDir = Path.Combine(paths.AdminByCategoryRoot, entry.Category);
            var targetFile = Path.Combine(targetDir, $"{entry.IconId}.png");

            if (!File.Exists(sourceFile))
            {
                messages.Add($"[missing-source] {sourceFile}");
                continue;
            }

            Directory.CreateDirectory(targetDir);
            if (File.Exists(targetFile) && !overwriteCurated)
            {
                skippedExisting++;
                manifestEntries.Add(ToManifestEntry(entry, copied: true, skipped: true));
                continue;
            }

            File.Copy(sourceFile, targetFile, overwrite: overwriteCurated);
            copied++;
            manifestEntries.Add(ToManifestEntry(entry, copied: true, skipped: false));
            messages.Add($"[copied] {entry.Category}/{entry.IconId}.png");
        }

        var manifest = new ItemPreviewCuratedCopyManifest(
            DateTimeOffset.UtcNow,
            "phase6c",
            sourceExportDirectory,
            paths.AdminByCategoryRoot,
            document.TotalCataloged,
            document.TotalPngExtracted,
            copied,
            skippedExisting,
            skippedLimit,
            weaponsCopied,
            max,
            approveCuratedCopy,
            overwriteCurated,
            manifestEntries
                .GroupBy(static e => e.Category, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(static g => g.Key, static g => g.ToList(), StringComparer.OrdinalIgnoreCase));

        var docsDir = Path.Combine(repoRoot, "docs", "admin-tools", "sprite-preview");
        Directory.CreateDirectory(docsDir);
        var manifestJsonPath = Path.Combine(docsDir, "item-preview-curated-copy-manifest-phase6c.json");
        var manifestMdPath = Path.Combine(docsDir, "item-preview-curated-copy-manifest-phase6c.md");
        var assetsManifestPath = Path.Combine(paths.AdminByCategoryRoot, "catalog-manifest.json");

        var json = JsonSerializer.Serialize(
            manifest,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        File.WriteAllText(manifestJsonPath, json, Encoding.UTF8);
        File.WriteAllText(assetsManifestPath, json, Encoding.UTF8);
        File.WriteAllText(manifestMdPath, WriteManifestMarkdown(manifest), Encoding.UTF8);

        return new ItemPreviewCopyResult(
            ordered.Count,
            copied,
            skippedExisting,
            skippedLimit,
            weaponsCopied,
            manifestJsonPath,
            manifestMdPath,
            assetsManifestPath,
            messages);
    }

    private static ItemPreviewCuratedManifestEntry ToManifestEntry(
        ItemPreviewExtractedEntry entry,
        bool copied,
        bool skipped) =>
        new(
            entry.ItemId,
            entry.IconId,
            entry.Category,
            entry.NameEs,
            entry.NameEn,
            $"/assets/item-previews/by-category/{entry.Category}/{entry.IconId}.png",
            copied,
            skipped);

    private static string WriteManifestMarkdown(ItemPreviewCuratedCopyManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Item preview curated copy manifest (Phase 6C)");
        builder.AppendLine();
        builder.AppendLine($"- Generado: `{manifest.GeneratedAtUtc:O}`");
        builder.AppendLine($"- Catalogados: **{manifest.TotalCataloged}**");
        builder.AppendLine($"- PNG extraidos (export): **{manifest.TotalPngExtracted}**");
        builder.AppendLine($"- PNG copiados a Angular: **{manifest.TotalCopiedToAngular}**");
        builder.AppendLine($"- Omitidos (ya existian): **{manifest.SkippedExisting}**");
        builder.AppendLine($"- Omitidos (limite): **{manifest.SkippedLimit}**");
        builder.AppendLine($"- Armas copiadas: **{manifest.WeaponsCopied}** (debe ser 0)");
        builder.AppendLine();
        builder.AppendLine("## Por categoría");
        foreach (var pair in manifest.ByCategory.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- `{pair.Key}`: {pair.Value.Count} entrada(s) en manifest");
        }

        return builder.ToString();
    }

    private static readonly JsonSerializerOptions CatalogJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static bool IsBlockedWeaponCategory(string category) =>
        category.Contains("arma", StringComparison.OrdinalIgnoreCase) ||
        category.Contains("weapon", StringComparison.OrdinalIgnoreCase);
}

internal sealed record ItemPreviewCuratedCopyManifest(
    DateTimeOffset GeneratedAtUtc,
    string Phase,
    string SourceExportDirectory,
    string AngularByCategoryRoot,
    int TotalCataloged,
    int TotalPngExtracted,
    int TotalCopiedToAngular,
    int SkippedExisting,
    int SkippedLimit,
    int WeaponsCopied,
    int CopyLimit,
    bool ApprovedCopy,
    bool OverwriteCurated,
    IReadOnlyDictionary<string, List<ItemPreviewCuratedManifestEntry>> ByCategory);

internal sealed record ItemPreviewCuratedManifestEntry(
    int ItemId,
    int IconId,
    string Category,
    string NameEs,
    string NameEn,
    string PreviewPath,
    bool Copied,
    bool SkippedExisting);
