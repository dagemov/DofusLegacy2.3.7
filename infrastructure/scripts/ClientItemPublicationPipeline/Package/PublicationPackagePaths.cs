namespace ClientItemPublicationPipeline.Package;

internal static class PublicationPackagePaths
{
    public const string ItemsRelative = "data/common/Items.d2o";
    public const string ItemSetsRelative = "data/common/ItemSets.d2o";
    public const string I18nEsRelative = "data/i18n/i18n_es.d2i";
    public const string I18nEnRelative = "data/i18n/i18n_en.d2i";
    public const string ManifestJson = "publication-package-manifest.json";
    public const string ManifestMarkdown = "publication-package-manifest.md";
    public const string ValidationJson = "validation-report.json";
    public const string ValidationMarkdown = "validation-report.md";
    public const string ChecksumsFile = "checksums.sha256";

    public static string ResolveItemsPath(string packageDirectory) =>
        ResolveExistingPath(packageDirectory, ItemsRelative, "Items.d2o");

    public static string ResolveI18nEsPath(string packageDirectory) =>
        ResolveExistingPath(packageDirectory, I18nEsRelative, "i18n_es.d2i");

    public static string ResolveI18nEnPath(string packageDirectory) =>
        ResolveExistingPath(packageDirectory, I18nEnRelative, "i18n_en.d2i");

    public static string? TryResolveItemSetsPath(string packageDirectory)
    {
        var structured = Path.Combine(packageDirectory, ItemSetsRelative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(structured))
        {
            return structured;
        }

        var legacy = Path.Combine(packageDirectory, "ItemSets.d2o");
        return File.Exists(legacy) ? legacy : null;
    }

    private static string ResolveExistingPath(string packageDirectory, string structuredRelative, string legacyFileName)
    {
        var structured = Path.Combine(packageDirectory, structuredRelative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(structured))
        {
            return structured;
        }

        var legacy = Path.Combine(packageDirectory, legacyFileName);
        return legacy;
    }
}
