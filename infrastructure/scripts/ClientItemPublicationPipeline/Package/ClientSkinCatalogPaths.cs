namespace ClientItemPublicationPipeline.Package;

internal sealed record ClientSkinCatalogPaths(
    string RepoRoot,
    string ClientRoot,
    string ItemsD2oPath,
    string ItemTypesD2oPath,
    string I18nEsPath,
    string I18nEnPath,
    string AdminByIconDirectory,
    string AdminByCategoryRoot,
    IReadOnlyList<string> BitmapD2pPaths)
{
    public static ClientSkinCatalogPaths Resolve(string repoRoot, string clientRoot)
    {
        var gfxItems = Path.Combine(clientRoot, "content", "gfx", "items");
        var bitmapD2p = Directory.Exists(gfxItems)
            ? Directory.GetFiles(gfxItems, "bitmap*.d2p", SearchOption.TopDirectoryOnly)
                .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();

        var adminRoot = Path.Combine(
            repoRoot,
            "Angular-tools",
            "Admin",
            "RollblackLegacy.Admin.Angular",
            "src",
            "assets",
            "item-previews");

        return new ClientSkinCatalogPaths(
            repoRoot,
            clientRoot,
            Path.Combine(clientRoot, "data", "common", "Items.d2o"),
            Path.Combine(clientRoot, "data", "common", "ItemTypes.d2o"),
            Path.Combine(clientRoot, "data", "i18n", "i18n_es.d2i"),
            Path.Combine(clientRoot, "data", "i18n", "i18n_en.d2i"),
            Path.Combine(adminRoot, "by-icon"),
            Path.Combine(adminRoot, "by-category"),
            bitmapD2p);
    }
}
