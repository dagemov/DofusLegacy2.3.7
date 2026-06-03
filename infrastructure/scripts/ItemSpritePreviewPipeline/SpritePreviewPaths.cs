namespace ItemSpritePreviewPipeline;

internal sealed record SpritePreviewPaths(
    string RepoRoot,
    string ClientRoot,
    string AdminAngularAssetsRoot,
    string ItemsD2oPath,
    string AppearancesD2oPath,
    string ByIconDirectory,
    string ByItemDirectory,
    string ByAppearanceDirectory,
    IReadOnlyList<string> ItemBitmapD2pPaths,
    IReadOnlyList<string> ItemVectorD2pPaths,
    string? LegacyItemBitmapDirectory)
{
    public static SpritePreviewPaths Resolve(string repoRoot)
    {
        var clientRoot = Path.Combine(repoRoot, "Client2.3.7");
        var adminAngularRoot = Path.Combine(repoRoot, "Angular-tools", "Admin", "RollblackLegacy.Admin.Angular");
        var assetsRoot = Path.Combine(adminAngularRoot, "src", "assets", "item-previews");
        var gfxItems = Path.Combine(clientRoot, "content", "gfx", "items");

        var bitmapD2p = Directory.Exists(gfxItems)
            ? Directory.GetFiles(gfxItems, "bitmap*.d2p", SearchOption.TopDirectoryOnly)
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();

        var vectorD2p = Directory.Exists(gfxItems)
            ? Directory.GetFiles(gfxItems, "vector*.d2p", SearchOption.TopDirectoryOnly)
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();

        var legacyBitmapCandidates = new[]
        {
            Path.Combine(repoRoot, "legacy-reference", "Rollback.Web", "client", "app", "content", "gfx", "items", "bitmap"),
            Path.Combine(repoRoot, "legacy-reference", "Rollback.Admin", "client", "app", "content", "gfx", "items", "bitmap")
        };

        string? legacyBitmap = null;
        foreach (var candidate in legacyBitmapCandidates)
        {
            if (Directory.Exists(candidate))
            {
                legacyBitmap = candidate;
                break;
            }
        }

        return new SpritePreviewPaths(
            repoRoot,
            clientRoot,
            assetsRoot,
            Path.Combine(clientRoot, "data", "common", "Items.d2o"),
            Path.Combine(clientRoot, "data", "common", "Appearances.d2o"),
            Path.Combine(assetsRoot, "by-icon"),
            Path.Combine(assetsRoot, "by-item"),
            Path.Combine(assetsRoot, "by-appearance"),
            bitmapD2p,
            vectorD2p,
            legacyBitmap);
    }

    public bool ClientDataPresent =>
        File.Exists(ItemsD2oPath) && ItemBitmapD2pPaths.Count > 0;

    public string DescribeD2pPacks() =>
        ItemBitmapD2pPaths.Count == 0
            ? "(bitmap D2P missing)"
            : string.Join(", ", ItemBitmapD2pPaths.Select(Path.GetFileName));
}
