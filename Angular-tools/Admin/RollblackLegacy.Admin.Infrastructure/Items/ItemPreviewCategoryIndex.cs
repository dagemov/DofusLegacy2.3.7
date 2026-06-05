using System.Text.Json;

namespace RollblackLegacy.Admin.Infrastructure.Items;

internal sealed class ItemPreviewCategoryIndex
{
    private readonly string _byCategoryRoot;
    private readonly Lazy<Dictionary<int, string>> _iconIdToCategory;
    private readonly Lazy<HashSet<int>> _allCategoryIconIds;

    public ItemPreviewCategoryIndex(string itemPreviewsRoot)
    {
        _byCategoryRoot = Path.Combine(itemPreviewsRoot, "by-category");
        _iconIdToCategory = new Lazy<Dictionary<int, string>>(BuildIconIndex);
        _allCategoryIconIds = new Lazy<HashSet<int>>(() => new HashSet<int>(_iconIdToCategory.Value.Keys));
    }

    public bool TryResolveCategoryPath(int iconId, out string webPath, out string physicalPath)
    {
        webPath = string.Empty;
        physicalPath = string.Empty;

        if (!_iconIdToCategory.Value.TryGetValue(iconId, out var category))
        {
            return false;
        }

        physicalPath = Path.Combine(_byCategoryRoot, category, $"{iconId}.png");
        if (!File.Exists(physicalPath))
        {
            return false;
        }

        webPath = $"/assets/item-previews/by-category/{category}/{iconId}.png";
        return true;
    }

    public int CountIndexedIcons() => _iconIdToCategory.Value.Count;

    private Dictionary<int, string> BuildIconIndex()
    {
        var index = new Dictionary<int, string>();
        LoadFromManifest(index);
        ScanFilesystem(index);
        return index;
    }

    private void LoadFromManifest(Dictionary<int, string> index)
    {
        var manifestPath = Path.Combine(_byCategoryRoot, "catalog-manifest.json");
        if (!File.Exists(manifestPath))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!document.RootElement.TryGetProperty("byCategory", out var byCategory))
            {
                return;
            }

            foreach (var categoryProperty in byCategory.EnumerateObject())
            {
                if (categoryProperty.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in categoryProperty.Value.EnumerateArray())
                {
                    if (!item.TryGetProperty("iconId", out var iconElement) ||
                        !iconElement.TryGetInt32(out var iconId) ||
                        iconId <= 0)
                    {
                        continue;
                    }

                    index[iconId] = categoryProperty.Name;
                }
            }
        }
        catch
        {
            // Manifest opcional; el escaneo de disco es la fuente de verdad.
        }
    }

    private void ScanFilesystem(Dictionary<int, string> index)
    {
        if (!Directory.Exists(_byCategoryRoot))
        {
            return;
        }

        foreach (var categoryDir in Directory.EnumerateDirectories(_byCategoryRoot))
        {
            var category = Path.GetFileName(categoryDir);
            if (string.IsNullOrWhiteSpace(category) || category.StartsWith(".", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var png in Directory.EnumerateFiles(categoryDir, "*.png", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileNameWithoutExtension(png);
                if (int.TryParse(fileName, out var iconId) && iconId > 0)
                {
                    index[iconId] = category;
                }
            }
        }
    }
}
