namespace ClientItemPublicationPipeline;

internal sealed record PublicationPipelineOptions(string Mode, int ItemId, string OutputDirectory)
{
    public static PublicationPipelineOptions Parse(string[] args)
    {
        var mode = "dry-run";
        var itemId = 12617;
        var output = "Infrastructure/temporal-artifacts/client-item-publication/12617";

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
            }
        }

        if (itemId <= 0)
        {
            throw new ArgumentException("--item-id debe ser un entero positivo.");
        }

        return new PublicationPipelineOptions(mode, itemId, output);
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
