using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ClientItemPublicationPipeline.Package;

internal sealed record ItemPreviewExpansionResult(
    string OutputDirectory,
    int NewPngExtracted,
    int PngSkippedExisting,
    int ExtractionErrors,
    int CopiedToAngular,
    int CopySkippedExisting,
    int TotalPngInAngular,
    IReadOnlyDictionary<string, int> NewByCategory,
    IReadOnlyDictionary<string, int> TotalByCategory,
    string ManifestPath,
    IReadOnlyList<string> Messages);

internal sealed class ItemPreviewCategoryExpansion
{
    public static readonly string[] Phase6dCategoryPriority =
    [
        "botas",
        "amuletos",
        "anillos",
        "escudos",
        "mascotas",
        "cinturones",
        "trofeos",
        "recursos",
        "consumibles"
    ];

    public static readonly string[] DefaultSkipCategories =
    [
        "dofus",
        "sombreros",
        "capas"
    ];

    public ItemPreviewExpansionResult Expand(
        string repoRoot,
        string outputDirectory,
        IReadOnlyList<string> targetCategories,
        IReadOnlyList<string> skipCategories,
        int newExtractionLimit,
        bool approveCuratedCopy,
        bool overwriteCurated)
    {
        var messages = new List<string>();
        var clientRoot = Path.Combine(repoRoot, "Client2.3.7");
        var paths = ClientSkinCatalogPaths.Resolve(repoRoot, clientRoot);
        var pngRoot = Path.Combine(outputDirectory, "png", "by-category");
        Directory.CreateDirectory(pngRoot);

        var builder = new ItemSkinCatalogBuilder();
        var catalog = builder.Build(paths, outputDirectory, excludeWeapons: true);

        var categorySet = new HashSet<string>(targetCategories, StringComparer.OrdinalIgnoreCase);
        var skipCategorySet = new HashSet<string>(skipCategories, StringComparer.OrdinalIgnoreCase);

        foreach (var populated in DetectPopulatedCategories(paths.AdminByCategoryRoot, skipCategories))
        {
            skipCategorySet.Add(populated);
            messages.Add($"[skip-category-populated] {populated}");
        }

        var candidates = catalog.Entries
            .Where(e => categorySet.Contains(e.Category))
            .Where(e => !skipCategorySet.Contains(e.Category))
            .Where(e => e.IconPreviewAvailable)
            .OrderBy(e => CategorySortKey(e.Category))
            .ThenBy(e => e.ItemId)
            .ToList();

        var seenIconPerCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newExtracted = new List<ItemPreviewExtractedEntry>();
        var errors = new List<ItemPreviewExtractionError>();
        var newByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var newPngExtracted = 0;
        var pngSkippedExisting = 0;

        void RecordNew(ItemSkinCatalogEntryDto entry, string targetFile, string source)
        {
            newExtracted.Add(new ItemPreviewExtractedEntry(
                entry.ItemId,
                entry.IconId,
                entry.Category,
                entry.NameEs,
                entry.NameEn,
                entry.TypeId,
                source,
                Path.GetRelativePath(outputDirectory, targetFile).Replace('\\', '/')));

            newByCategory.TryGetValue(entry.Category, out var count);
            newByCategory[entry.Category] = count + 1;
        }

        foreach (var entry in candidates)
        {
            if (newPngExtracted >= newExtractionLimit)
            {
                break;
            }

            var dedupeKey = $"{entry.Category}:{entry.IconId}";
            if (!seenIconPerCategory.Add(dedupeKey))
            {
                continue;
            }

            var angularTarget = Path.Combine(paths.AdminByCategoryRoot, entry.Category, $"{entry.IconId}.png");
            var exportTarget = Path.Combine(pngRoot, entry.Category, $"{entry.IconId}.png");

            if (File.Exists(angularTarget) || File.Exists(exportTarget))
            {
                pngSkippedExisting++;
                continue;
            }

            if (CatalogD2pIconResolver.TryExtractPng(paths.BitmapD2pPaths, entry.IconId, exportTarget))
            {
                newPngExtracted++;
                RecordNew(entry, exportTarget, "client-bitmap-d2p");
                continue;
            }

            var adminIcon = Path.Combine(paths.AdminByIconDirectory, $"{entry.IconId}.png");
            if (File.Exists(adminIcon))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(exportTarget)!);
                File.Copy(adminIcon, exportTarget, overwrite: false);
                newPngExtracted++;
                RecordNew(entry, exportTarget, "admin-by-icon-fallback");
                continue;
            }

            errors.Add(new ItemPreviewExtractionError(entry.ItemId, entry.IconId, entry.Category, "PNG no encontrado en D2P ni admin-by-icon"));
        }

        var catalogJsonPath = Path.Combine(outputDirectory, "catalog.json");
        var catalogCsvPath = Path.Combine(outputDirectory, "catalog.csv");
        var galleryHtmlPath = Path.Combine(outputDirectory, "gallery.html");
        var expansionDocument = new ItemPreviewCatalogDocument(
            DateTimeOffset.UtcNow,
            "item-preview-expand-categories",
            catalog.Summary.CatalogEntries,
            newPngExtracted,
            catalog.Summary.SkippedWeapons,
            newExtractionLimit,
            targetCategories,
            newExtracted,
            errors,
            catalog.Summary.Categories);

        File.WriteAllText(catalogJsonPath, JsonSerializer.Serialize(expansionDocument, JsonOptions), Encoding.UTF8);
        File.WriteAllText(catalogCsvPath, WriteCsv(newExtracted), Encoding.UTF8);
        ItemPreviewExportGalleryGenerator.Generate(galleryHtmlPath, newExtracted, pngRoot);

        var copied = 0;
        var copySkipped = 0;
        if (approveCuratedCopy)
        {
            foreach (var entry in newExtracted)
            {
                var sourceFile = Path.Combine(pngRoot, entry.Category, $"{entry.IconId}.png");
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
                    copySkipped++;
                    continue;
                }

                File.Copy(sourceFile, targetFile, overwrite: overwriteCurated);
                copied++;
                messages.Add($"[copied] {entry.Category}/{entry.IconId}.png");
            }
        }
        else
        {
            messages.Add("[copy-blocked] requiere --approve-curated-copy");
        }

        var totalByCategory = CountPngByCategory(paths.AdminByCategoryRoot);
        var assetsManifestPath = Path.Combine(paths.AdminByCategoryRoot, "catalog-manifest.json");
        var manifest = ItemPreviewManifestMerger.Merge(
            assetsManifestPath,
            newExtracted,
            catalog.Summary.CatalogEntries,
            catalog.Summary.SkippedWeapons,
            totalByCategory,
            copied,
            copySkipped,
            pngSkippedExisting,
            errors.Count,
            outputDirectory,
            paths.AdminByCategoryRoot,
            "phase6d",
            approveCuratedCopy);

        var docsDir = Path.Combine(repoRoot, "docs", "admin-tools", "sprite-preview");
        Directory.CreateDirectory(docsDir);
        var docsManifestPath = Path.Combine(docsDir, "item-preview-curated-copy-manifest-phase6d.json");
        var docsMdPath = Path.Combine(docsDir, "item-preview-curated-copy-manifest-phase6d.md");
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(assetsManifestPath, json, Encoding.UTF8);
        File.WriteAllText(docsManifestPath, json, Encoding.UTF8);
        File.WriteAllText(docsMdPath, WriteManifestMarkdown(manifest), Encoding.UTF8);

        return new ItemPreviewExpansionResult(
            outputDirectory,
            newPngExtracted,
            pngSkippedExisting,
            errors.Count,
            copied,
            copySkipped,
            totalByCategory.Values.Sum(),
            newByCategory,
            totalByCategory,
            assetsManifestPath,
            messages);
    }

    private static IEnumerable<string> DetectPopulatedCategories(string byCategoryRoot, IReadOnlyList<string> candidates)
    {
        if (!Directory.Exists(byCategoryRoot))
        {
            yield break;
        }

        foreach (var category in candidates)
        {
            var dir = Path.Combine(byCategoryRoot, category);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            if (Directory.EnumerateFiles(dir, "*.png", SearchOption.TopDirectoryOnly).Any())
            {
                yield return category;
            }
        }
    }

    private static Dictionary<string, int> CountPngByCategory(string byCategoryRoot)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(byCategoryRoot))
        {
            return counts;
        }

        foreach (var dir in Directory.EnumerateDirectories(byCategoryRoot))
        {
            var category = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(category) || category.StartsWith(".", StringComparison.Ordinal))
            {
                continue;
            }

            counts[category] = Directory.EnumerateFiles(dir, "*.png", SearchOption.TopDirectoryOnly).Count();
        }

        return counts;
    }

    private static int CategorySortKey(string category)
    {
        for (var index = 0; index < Phase6dCategoryPriority.Length; index++)
        {
            if (string.Equals(Phase6dCategoryPriority[index], category, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return Phase6dCategoryPriority.Length + 1;
    }

    private static string WriteCsv(IReadOnlyList<ItemPreviewExtractedEntry> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("ItemId,IconId,Category,NameEs,NameEn,TypeId,IconSource,PngRelativePath");
        foreach (var entry in entries)
        {
            builder.AppendLine(string.Join(
                ",",
                entry.ItemId.ToString(CultureInfo.InvariantCulture),
                entry.IconId.ToString(CultureInfo.InvariantCulture),
                CsvEscape(entry.Category),
                CsvEscape(entry.NameEs),
                CsvEscape(entry.NameEn),
                entry.TypeId.ToString(CultureInfo.InvariantCulture),
                CsvEscape(entry.IconSource),
                CsvEscape(entry.PngRelativePath)));
        }

        return builder.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private static string WriteManifestMarkdown(ItemPreviewCatalogManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Item preview catalog manifest (Phase 6D)");
        builder.AppendLine();
        builder.AppendLine($"- Generado: `{manifest.GeneratedAtUtc:O}`");
        builder.AppendLine($"- Fuente preview: `{manifest.PreviewSource}`");
        builder.AppendLine($"- PNG en Angular: **{manifest.TotalPngInAngular}**");
        builder.AppendLine($"- Armas excluidas (catálogo): **{manifest.WeaponsExcluded}**");
        builder.AppendLine();
        builder.AppendLine("## Por categoría");
        foreach (var pair in manifest.CategoryStats.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine(
                $"- `{pair.Key}`: **{pair.Value.Count}** (última extracción `{pair.Value.LastExtractionUtc:O}`)");
        }

        return builder.ToString();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
