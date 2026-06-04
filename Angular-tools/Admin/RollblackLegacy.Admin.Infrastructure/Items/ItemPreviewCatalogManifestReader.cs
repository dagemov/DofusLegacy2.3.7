using System.Text.Json;

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
