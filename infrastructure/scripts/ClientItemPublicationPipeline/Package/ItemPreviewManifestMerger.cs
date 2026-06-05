using System.Text.Json;

namespace ClientItemPublicationPipeline.Package;

internal sealed record ItemPreviewCategoryStat(
    int Count,
    DateTimeOffset LastExtractionUtc,
    string PreviewSource);

internal sealed record ItemPreviewCatalogManifest(
    DateTimeOffset GeneratedAtUtc,
    string Phase,
    string PreviewSource,
    string? SourceExportDirectory,
    string AngularByCategoryRoot,
    int TotalCataloged,
    int TotalPngInAngular,
    int WeaponsExcluded,
    int LastRunNewExtracted,
    int LastRunCopied,
    int LastRunSkippedExisting,
    int LastRunExtractionErrors,
    bool ApprovedCopy,
    IReadOnlyDictionary<string, ItemPreviewCategoryStat> CategoryStats,
    IReadOnlyDictionary<string, List<ItemPreviewCuratedManifestEntry>> ByCategory);

internal static class ItemPreviewManifestMerger
{
    private const string DefaultPreviewSource = "client-bitmap-d2p";

    public static ItemPreviewCatalogManifest Merge(
        string existingManifestPath,
        IReadOnlyList<ItemPreviewExtractedEntry> newEntries,
        int totalCataloged,
        int weaponsExcluded,
        IReadOnlyDictionary<string, int> totalByCategory,
        int lastRunCopied,
        int lastRunSkippedExisting,
        int lastRunSkippedDuringExtract,
        int lastRunErrors,
        string sourceExportDirectory,
        string angularByCategoryRoot,
        string phase,
        bool approvedCopy)
    {
        var now = DateTimeOffset.UtcNow;
        var byCategory = LoadExistingByCategory(existingManifestPath);
        var categoryStats = LoadExistingCategoryStats(existingManifestPath);

        foreach (var entry in newEntries)
        {
            var manifestEntry = new ItemPreviewCuratedManifestEntry(
                entry.ItemId,
                entry.IconId,
                entry.Category,
                entry.NameEs,
                entry.NameEn,
                $"/assets/item-previews/by-category/{entry.Category}/{entry.IconId}.png",
                Copied: true,
                SkippedExisting: false);

            if (!byCategory.TryGetValue(entry.Category, out var list))
            {
                list = [];
                byCategory[entry.Category] = list;
            }

            if (!list.Any(e => e.IconId == entry.IconId))
            {
                list.Add(manifestEntry);
            }
        }

        foreach (var pair in totalByCategory)
        {
            categoryStats[pair.Key] = new ItemPreviewCategoryStat(
                pair.Value,
                now,
                DefaultPreviewSource);
        }

        foreach (var pair in byCategory)
        {
            if (!categoryStats.ContainsKey(pair.Key))
            {
                categoryStats[pair.Key] = new ItemPreviewCategoryStat(
                    pair.Value.Count,
                    now,
                    DefaultPreviewSource);
            }
            else
            {
                var existing = categoryStats[pair.Key];
                categoryStats[pair.Key] = existing with { Count = totalByCategory.TryGetValue(pair.Key, out var c) ? c : pair.Value.Count };
            }
        }

        return new ItemPreviewCatalogManifest(
            now,
            phase,
            DefaultPreviewSource,
            sourceExportDirectory,
            angularByCategoryRoot,
            totalCataloged,
            totalByCategory.Values.Sum(),
            weaponsExcluded,
            newEntries.Count,
            lastRunCopied,
            lastRunSkippedExisting + lastRunSkippedDuringExtract,
            lastRunErrors,
            approvedCopy,
            categoryStats,
            byCategory);
    }

    private static Dictionary<string, List<ItemPreviewCuratedManifestEntry>> LoadExistingByCategory(string path)
    {
        var result = new Dictionary<string, List<ItemPreviewCuratedManifestEntry>>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            return result;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("byCategory", out var byCategory))
        {
            return result;
        }

        foreach (var categoryProperty in byCategory.EnumerateObject())
        {
            if (categoryProperty.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var list = new List<ItemPreviewCuratedManifestEntry>();
            foreach (var item in categoryProperty.Value.EnumerateArray())
            {
                var itemId = ReadInt(item, "itemId");
                var iconId = ReadInt(item, "iconId");
                if (iconId <= 0)
                {
                    continue;
                }

                list.Add(new ItemPreviewCuratedManifestEntry(
                    itemId > 0 ? itemId : 0,
                    iconId,
                    ReadString(item, "category") ?? categoryProperty.Name,
                    ReadString(item, "nameEs") ?? string.Empty,
                    ReadString(item, "nameEn") ?? string.Empty,
                    ReadString(item, "previewPath")
                        ?? $"/assets/item-previews/by-category/{categoryProperty.Name}/{iconId}.png",
                    ReadBool(item, "copied"),
                    ReadBool(item, "skippedExisting")));
            }

            result[categoryProperty.Name] = list;
        }

        return result;
    }

    private static Dictionary<string, ItemPreviewCategoryStat> LoadExistingCategoryStats(string path)
    {
        var result = new Dictionary<string, ItemPreviewCategoryStat>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            return result;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("categoryStats", out var stats))
        {
            return result;
        }

        foreach (var property in stats.EnumerateObject())
        {
            var element = property.Value;
            result[property.Name] = new ItemPreviewCategoryStat(
                ReadInt(element, "count"),
                ReadDate(element, "lastExtractionUtc"),
                ReadString(element, "previewSource") ?? DefaultPreviewSource);
        }

        return result;
    }

    private static int ReadInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool ReadBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) &&
        (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) &&
        value.GetBoolean();

    private static DateTimeOffset ReadDate(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }
}
