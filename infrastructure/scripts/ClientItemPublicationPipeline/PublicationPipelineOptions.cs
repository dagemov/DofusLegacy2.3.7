namespace ClientItemPublicationPipeline;

internal sealed record PublicationPipelineOptions(
    string Mode,
    int ItemId,
    string OutputDirectory,
    string? D2oClassName,
    int SourceItemId,
    int TargetItemId,
    int CloneTypeId,
    int CloneIconId,
    int CloneAppearanceId)
{
    public static PublicationPipelineOptions Parse(string[] args)
    {
        var mode = "dry-run";
        var itemId = 12617;
        var output = "Infrastructure/temporal-artifacts/client-item-publication/12617";
        string? d2oClassName = "Item";
        var sourceItemId = 7754;
        var targetItemId = 12617;
        var cloneTypeId = 23;
        var cloneIconId = 23012;
        var cloneAppearanceId = 0;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--mode" when index + 1 < args.Length:
                    mode = args[++index];
                    break;
                case "--item-id" when index + 1 < args.Length:
                    itemId = int.Parse(args[++index]);
                    break;
                case "--output" when index + 1 < args.Length:
                    output = args[++index];
                    break;
                case "--class" when index + 1 < args.Length:
                    d2oClassName = args[++index];
                    break;
                case "--source-item-id" when index + 1 < args.Length:
                    sourceItemId = int.Parse(args[++index]);
                    break;
                case "--target-item-id" when index + 1 < args.Length:
                    targetItemId = int.Parse(args[++index]);
                    break;
                case "--clone-type-id" when index + 1 < args.Length:
                    cloneTypeId = int.Parse(args[++index]);
                    break;
                case "--clone-icon-id" when index + 1 < args.Length:
                    cloneIconId = int.Parse(args[++index]);
                    break;
                case "--clone-appearance-id" when index + 1 < args.Length:
                    cloneAppearanceId = int.Parse(args[++index]);
                    break;
            }
        }

        if (itemId <= 0 && mode.Equals("dry-run", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("--item-id debe ser un entero positivo.");
        }

        return new PublicationPipelineOptions(
            mode,
            itemId,
            output,
            d2oClassName,
            sourceItemId,
            targetItemId,
            cloneTypeId,
            cloneIconId,
            cloneAppearanceId);
    }
}

internal static class RepositoryRootResolver
{
    public static string Resolve(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var hasAdmin = Directory.Exists(Path.Combine(directory.FullName, "Angular-tools", "Admin"));
            var hasDocs = Directory.Exists(Path.Combine(directory.FullName, "docs"));
            if (hasAdmin && hasDocs)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se pudo resolver la raiz del repo desde ClientItemPublicationPipeline.");
    }
}

internal sealed record RepositoryPaths(string RepoRoot, string AdminApiConfigDirectory)
{
    public static RepositoryPaths FromRepoRoot(string repoRoot) =>
        new(repoRoot, Path.Combine(repoRoot, "Angular-tools", "Admin", "RollblackLegacy.Admin.Api"));
}
