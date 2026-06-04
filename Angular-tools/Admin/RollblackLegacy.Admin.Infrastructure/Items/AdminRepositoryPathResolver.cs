namespace RollblackLegacy.Admin.Infrastructure.Items;

public static class AdminRepositoryPathResolver
{
    private static readonly string[][] AdminAngularAssetsSegmentsCandidates =
    [
        ["Angular-tools", "Admin", "RollblackLegacy.Admin.Angular", "src", "assets"],
        ["src", "Admin", "RollblackLegacy.Admin.Angular", "src", "assets"]
    ];

    public static string ResolveRepositoryRoot(string contentRootPath)
    {
        var directory = new DirectoryInfo(contentRootPath);

        while (directory is not null)
        {
            var hasAngularTools = Directory.Exists(Path.Combine(directory.FullName, "Angular-tools"));
            var hasSrc = Directory.Exists(Path.Combine(directory.FullName, "src"));
            var hasDocs = Directory.Exists(Path.Combine(directory.FullName, "docs"));

            if ((hasAngularTools || hasSrc) && hasDocs)
                return directory.FullName;

            directory = directory.Parent;
        }

        return contentRootPath;
    }

    public static string ResolveAdminAngularAssetsRoot(string contentRootPath)
    {
        var repositoryRoot = ResolveRepositoryRoot(contentRootPath);

        foreach (var segments in AdminAngularAssetsSegmentsCandidates)
        {
            var candidate = Path.Combine([repositoryRoot, .. segments]);
            if (Directory.Exists(candidate))
                return candidate;
        }

        return Path.Combine([repositoryRoot, .. AdminAngularAssetsSegmentsCandidates[0]]);
    }

    public static string ResolveAdminAngularItemPreviewsRoot(string contentRootPath) =>
        Path.Combine(ResolveAdminAngularAssetsRoot(contentRootPath), "item-previews");

    public static string ResolveAdminAngularByIconRoot(string contentRootPath) =>
        Path.Combine(ResolveAdminAngularItemPreviewsRoot(contentRootPath), "by-icon");

    public static string ResolveAdminAngularByAppearanceRoot(string contentRootPath) =>
        Path.Combine(ResolveAdminAngularItemPreviewsRoot(contentRootPath), "by-appearance");

    public static string ResolveAdminAngularManualItemsRoot(string contentRootPath) =>
        Path.Combine(ResolveAdminAngularAssetsRoot(contentRootPath), "manual-assets", "items");
}
