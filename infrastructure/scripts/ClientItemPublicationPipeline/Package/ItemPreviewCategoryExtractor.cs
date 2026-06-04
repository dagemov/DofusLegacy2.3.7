using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ClientItemPublicationPipeline.Package;

internal sealed record ItemPreviewExtractResult(
    string OutputDirectory,
    string CatalogJsonPath,
    string CatalogCsvPath,
    string GalleryHtmlPath,
    int CatalogedEntries,
    int PngExtracted,
    int ExtractionErrors,
    int WeaponsExcluded,
    IReadOnlyDictionary<string, int> ExtractedByCategory);

internal sealed class ItemPreviewCategoryExtractor
{
    private static readonly string[] CategoryPriority =
    [
        "dofus",
        "sombreros",
        "capas",
        "botas",
        "mascotas",
        "escudos",
        "anillos",
        "amuletos",
        "cinturones",
        "recursos"
    ];

    public ItemPreviewExtractResult Extract(
        ClientSkinCatalogPaths paths,
        string outputDirectory,
        IReadOnlyList<string> categories,
        int limit,
        bool excludeWeapons)
    {
        Directory.CreateDirectory(outputDirectory);
        var pngRoot = Path.Combine(outputDirectory, "png", "by-category");
        Directory.CreateDirectory(pngRoot);

        var builder = new ItemSkinCatalogBuilder();
        var catalog = builder.Build(paths, outputDirectory, excludeWeapons);

        var categorySet = new HashSet<string>(
            categories.Count > 0 ? categories : ItemSkinCategoryMap.ExportCategories,
            StringComparer.OrdinalIgnoreCase);

        var candidates = catalog.Entries
            .Where(e => categorySet.Contains(e.Category))
            .Where(e => e.IconPreviewAvailable)
            .OrderBy(e => CategorySortKey(e.Category))
            .ThenBy(e => e.ItemId)
            .ToList();

        var seenIconPerCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extracted = new List<ItemPreviewExtractedEntry>();
        var errors = new List<ItemPreviewExtractionError>();
        var extractedByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var pngExtracted = 0;

        foreach (var entry in candidates)
        {
            if (pngExtracted >= limit)
            {
                break;
            }

            var dedupeKey = $"{entry.Category}:{entry.IconId}";
            if (!seenIconPerCategory.Add(dedupeKey))
            {
                continue;
            }

            var targetFile = Path.Combine(pngRoot, entry.Category, $"{entry.IconId}.png");
            if (File.Exists(targetFile))
            {
                pngExtracted++;
                RecordExtracted(entry, targetFile, "existing-on-disk");
                continue;
            }

            if (CatalogD2pIconResolver.TryExtractPng(paths.BitmapD2pPaths, entry.IconId, targetFile))
            {
                pngExtracted++;
                RecordExtracted(entry, targetFile, "client-bitmap-d2p");
                continue;
            }

            var adminIcon = Path.Combine(paths.AdminByIconDirectory, $"{entry.IconId}.png");
            if (File.Exists(adminIcon))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                File.Copy(adminIcon, targetFile, overwrite: false);
                pngExtracted++;
                RecordExtracted(entry, targetFile, "admin-by-icon-fallback");
                continue;
            }

            errors.Add(new ItemPreviewExtractionError(entry.ItemId, entry.IconId, entry.Category, "PNG no encontrado en D2P ni admin-by-icon"));
        }

        var catalogJsonPath = Path.Combine(outputDirectory, "catalog.json");
        var catalogCsvPath = Path.Combine(outputDirectory, "catalog.csv");
        var galleryHtmlPath = Path.Combine(outputDirectory, "gallery.html");

        var payload = new ItemPreviewCatalogDocument(
            DateTimeOffset.UtcNow,
            "item-preview-extract-by-category",
            catalog.Summary.CatalogEntries,
            pngExtracted,
            catalog.Summary.SkippedWeapons,
            limit,
            categories.Count > 0 ? categories : ItemSkinCategoryMap.ExportCategories,
            extracted,
            errors,
            catalog.Summary.Categories);

        File.WriteAllText(catalogJsonPath, JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8);
        File.WriteAllText(catalogCsvPath, WriteCsv(extracted), Encoding.UTF8);
        ItemPreviewExportGalleryGenerator.Generate(galleryHtmlPath, extracted, pngRoot);

        return new ItemPreviewExtractResult(
            outputDirectory,
            catalogJsonPath,
            catalogCsvPath,
            galleryHtmlPath,
            catalog.Summary.CatalogEntries,
            pngExtracted,
            errors.Count,
            catalog.Summary.SkippedWeapons,
            extractedByCategory);

        void RecordExtracted(ItemSkinCatalogEntryDto entry, string targetFile, string source)
        {
            extracted.Add(new ItemPreviewExtractedEntry(
                entry.ItemId,
                entry.IconId,
                entry.Category,
                entry.NameEs,
                entry.NameEn,
                entry.TypeId,
                source,
                ToRelativePngPath(targetFile, outputDirectory)));

            extractedByCategory.TryGetValue(entry.Category, out var count);
            extractedByCategory[entry.Category] = count + 1;
        }
    }

    private static string ToRelativePngPath(string absolutePath, string outputDirectory) =>
        Path.GetRelativePath(outputDirectory, absolutePath).Replace('\\', '/');

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

    private static int CategorySortKey(string category)
    {
        for (var index = 0; index < CategoryPriority.Length; index++)
        {
            if (string.Equals(CategoryPriority[index], category, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return CategoryPriority.Length + 1;
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

internal sealed record ItemPreviewExtractedEntry(
    int ItemId,
    int IconId,
    string Category,
    string NameEs,
    string NameEn,
    int TypeId,
    string IconSource,
    string PngRelativePath);

internal sealed record ItemPreviewExtractionError(int ItemId, int IconId, string Category, string Error);

internal sealed record ItemPreviewCatalogDocument(
    DateTimeOffset GeneratedAtUtc,
    string Mode,
    int TotalCataloged,
    int TotalPngExtracted,
    int WeaponsExcluded,
    int ExtractionLimit,
    IReadOnlyList<string> RequestedCategories,
    IReadOnlyList<ItemPreviewExtractedEntry> Extracted,
    IReadOnlyList<ItemPreviewExtractionError> ExtractionErrors,
    IReadOnlyDictionary<string, int> CatalogCountsByCategory);
