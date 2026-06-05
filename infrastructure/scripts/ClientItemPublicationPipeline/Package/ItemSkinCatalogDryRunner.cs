namespace ClientItemPublicationPipeline.Package;

/// <summary>Compatibilidad Phase 6 — delega en <see cref="ItemSkinCatalogBuilder"/>.</summary>
internal sealed class ItemSkinCatalogDryRunner
{
    public ItemSkinCatalogDryRunResult Run(
        string repoRoot,
        string outputDirectory,
        string clientRoot,
        bool excludeWeapons = true)
    {
        var paths = ClientSkinCatalogPaths.Resolve(repoRoot, clientRoot);
        var builder = new ItemSkinCatalogBuilder();
        var result = builder.Build(paths, outputDirectory, excludeWeapons);

        var galleryRoot = Directory.GetParent(outputDirectory)?.FullName ?? outputDirectory;
        var galleryPath = ItemSkinCatalogGalleryGenerator.Generate(
            Path.Combine(galleryRoot, "gallery"),
            result.Entries,
            paths.AdminByIconDirectory);

        return new ItemSkinCatalogDryRunResult(
            result.OutputDirectory,
            result.JsonPath,
            result.MarkdownPath,
            galleryPath,
            MapSummary(result.Summary));
    }

    private static ItemSkinCatalogSummary MapSummary(ItemSkinCatalogSummaryDto summary) =>
        new(
            summary.GeneratedAtUtc,
            summary.TotalIndexEntries,
            summary.CatalogEntries,
            summary.SkippedWeapons,
            summary.SkippedUnreadable,
            summary.WithIconPreview,
            summary.Categories,
            ItemSkinCategoryMap.PlannedAngularFolders);
}

internal sealed record ItemSkinCatalogSummary(
    DateTimeOffset GeneratedAtUtc,
    int TotalIndexEntries,
    int CatalogEntries,
    int SkippedWeapons,
    int SkippedUnreadable,
    int WithIconPreview,
    IReadOnlyDictionary<string, int> Categories,
    IReadOnlyList<string> PlannedAngularFolders);

internal sealed record ItemSkinCatalogDryRunResult(
    string OutputDirectory,
    string JsonPath,
    string MarkdownPath,
    string GalleryHtmlPath,
    ItemSkinCatalogSummary Summary);
