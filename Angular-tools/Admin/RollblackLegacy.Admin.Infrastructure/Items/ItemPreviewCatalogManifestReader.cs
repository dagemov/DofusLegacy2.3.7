using System.Text.Json;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Infrastructure.Items;

internal sealed record ItemPreviewManifestEntry(
    int ItemId,
    int IconId,
    string Category,
    string NameEs,
    string NameEn,
    string PreviewPath,
    bool Copied,
    bool SkippedExisting);

internal sealed class ItemPreviewCatalogManifestReader
{
    private static readonly Dictionary<string, string> CategoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dofus"] = "Dofus",
        ["sombreros"] = "Sombreros",
        ["capas"] = "Capas",
        ["botas"] = "Botas",
        ["mascotas"] = "Mascotas",
        ["escudos"] = "Escudos",
        ["anillos"] = "Anillos",
        ["amuletos"] = "Amuletos",
        ["cinturones"] = "Cinturones",
        ["recursos"] = "Recursos",
        ["trofeos"] = "Trofeos",
        ["consumibles"] = "Consumibles"
    };

    public ItemIconCategoryStatsDto LoadCategoryStats(string manifestPath, string byCategoryRoot)
    {
        var fileCounts = CountPngFiles(byCategoryRoot);
        if (!File.Exists(manifestPath))
        {
            return BuildFromFileCounts(fileCounts, null);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        var previewSource = ReadRootString(root, "previewSource") ?? "client-bitmap-d2p";
        var totalCataloged = ReadRootInt(root, "totalCataloged");
        var weaponsExcluded = ReadRootInt(root, "weaponsExcluded");
        var totalInAngular = ReadRootInt(root, "totalPngInAngular");

        var categories = new List<ItemIconCategoryStatDto>();
        if (root.TryGetProperty("categoryStats", out var categoryStats))
        {
            foreach (var property in categoryStats.EnumerateObject())
            {
                var element = property.Value;
                var count = ReadInt(element, "count");
                if (count <= 0 && fileCounts.TryGetValue(property.Name, out var fileCount))
                {
                    count = fileCount;
                }

                categories.Add(new ItemIconCategoryStatDto(
                    property.Name,
                    ResolveLabel(property.Name),
                    count,
                    ReadDate(element, "lastExtractionUtc"),
                    ReadString(element, "previewSource") ?? previewSource));
            }
        }

        foreach (var pair in fileCounts)
        {
            if (categories.Any(c => string.Equals(c.Category, pair.Key, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            categories.Add(new ItemIconCategoryStatDto(
                pair.Key,
                ResolveLabel(pair.Key),
                pair.Value,
                null,
                previewSource));
        }

        if (totalInAngular <= 0)
        {
            totalInAngular = fileCounts.Values.Sum();
        }

        return new ItemIconCategoryStatsDto(
            totalInAngular,
            totalCataloged,
            weaponsExcluded,
            previewSource,
            categories.OrderBy(static c => c.Category, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public IReadOnlyList<ItemPreviewManifestEntry> LoadEntries(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return [];
        }

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (!document.RootElement.TryGetProperty("byCategory", out var byCategory) &&
            !document.RootElement.TryGetProperty("ByCategory", out byCategory))
        {
            return [];
        }

        var entries = new List<ItemPreviewManifestEntry>();
        foreach (var categoryProperty in byCategory.EnumerateObject())
        {
            if (categoryProperty.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in categoryProperty.Value.EnumerateArray())
            {
                var itemId = ReadInt(item, "itemId", "ItemId");
                var iconId = ReadInt(item, "iconId", "IconId");
                if (itemId <= 0 || iconId <= 0)
                {
                    continue;
                }

                entries.Add(new ItemPreviewManifestEntry(
                    itemId,
                    iconId,
                    ReadString(item, "category", "Category") ?? categoryProperty.Name,
                    ReadString(item, "nameEs", "NameEs") ?? string.Empty,
                    ReadString(item, "nameEn", "NameEn") ?? string.Empty,
                    ReadString(item, "previewPath", "PreviewPath")
                        ?? $"/assets/item-previews/by-category/{categoryProperty.Name}/{iconId}.png",
                    ReadBool(item, "copied", "Copied"),
                    ReadBool(item, "skippedExisting", "SkippedExisting")));
            }
        }

        return entries;
    }

    private static int ReadInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static ItemIconCategoryStatsDto BuildFromFileCounts(
        Dictionary<string, int> fileCounts,
        JsonElement? root)
    {
        var categories = fileCounts
            .Select(pair => new ItemIconCategoryStatDto(
                pair.Key,
                ResolveLabel(pair.Key),
                pair.Value,
                null,
                "client-bitmap-d2p"))
            .OrderBy(static c => c.Category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ItemIconCategoryStatsDto(
            fileCounts.Values.Sum(),
            0,
            0,
            "client-bitmap-d2p",
            categories);
    }

    private static Dictionary<string, int> CountPngFiles(string byCategoryRoot)
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

    private static string ResolveLabel(string category) =>
        CategoryLabels.TryGetValue(category, out var label) ? label : category;

    private static int ReadRootInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private static string? ReadRootString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTimeOffset? ReadDate(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool ReadBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) &&
                (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
            {
                return value.GetBoolean();
            }
        }

        return false;
    }
}
