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
    int CloneAppearanceId,
    string EsName,
    string EsDescription,
    string EnName,
    string EnDescription,
    bool StagePublicationPackage,
    string? PackageDirectory,
    string? SandboxDirectory,
    string? ClientDirectory,
    string? ExcludeTypes,
    string? Category,
    int CatalogLimit,
    bool CatalogDryRun,
    bool ApproveCuratedCopy,
    string? Categories,
    string? SourceDirectory,
    bool OverwriteCuratedCopy)
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
        var esName = "Dofus de los Hielos";
        var esDescription = "Dofus de los Hielos creado para pruebas controladas del pipeline de publicación.";
        var enName = "Ice Dofus";
        var enDescription = "Ice Dofus created for controlled publication pipeline testing.";
        var stagePublicationPackage = false;
        string? packageDirectory = null;
        string? sandboxDirectory = null;
        string? clientDirectory = null;
        var excludeTypes = "weapons";
        string? category = null;
        var catalogLimit = 50;
        var catalogDryRun = false;
        var approveCuratedCopy = false;
        string? categories = null;
        string? sourceDirectory = null;
        var overwriteCuratedCopy = false;

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
                case "--es-name" when index + 1 < args.Length:
                    esName = args[++index];
                    break;
                case "--es-description" when index + 1 < args.Length:
                    esDescription = args[++index];
                    break;
                case "--en-name" when index + 1 < args.Length:
                    enName = args[++index];
                    break;
                case "--en-description" when index + 1 < args.Length:
                    enDescription = args[++index];
                    break;
                case "--stage-publication-package":
                    stagePublicationPackage = true;
                    break;
                case "--package" when index + 1 < args.Length:
                    packageDirectory = args[++index];
                    break;
                case "--sandbox" when index + 1 < args.Length:
                    sandboxDirectory = args[++index];
                    break;
                case "--client" when index + 1 < args.Length:
                    clientDirectory = args[++index];
                    break;
                case "--exclude-types" when index + 1 < args.Length:
                    excludeTypes = args[++index];
                    break;
                case "--category" when index + 1 < args.Length:
                    category = args[++index];
                    break;
                case "--limit" when index + 1 < args.Length:
                    catalogLimit = int.Parse(args[++index]);
                    break;
                case "--dry-run":
                    catalogDryRun = true;
                    break;
                case "--approve-curated-copy":
                    approveCuratedCopy = true;
                    break;
                case "--categories" when index + 1 < args.Length:
                    categories = args[++index];
                    break;
                case "--source" when index + 1 < args.Length:
                    sourceDirectory = args[++index];
                    break;
                case "--overwrite-curated":
                    overwriteCuratedCopy = true;
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
            cloneAppearanceId,
            esName,
            esDescription,
            enName,
            enDescription,
            stagePublicationPackage,
            packageDirectory,
            sandboxDirectory,
            clientDirectory,
            excludeTypes,
            category,
            catalogLimit,
            catalogDryRun,
            approveCuratedCopy,
            categories,
            sourceDirectory,
            overwriteCuratedCopy);
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

    public string ClientI18nEsPath => Path.Combine(RepoRoot, "Client2.3.7", "data", "i18n", "i18n_es.d2i");

    public string ClientI18nEnPath => Path.Combine(RepoRoot, "Client2.3.7", "data", "i18n", "i18n_en.d2i");

    public string ClientItemsD2oPath => Path.Combine(RepoRoot, "Client2.3.7", "data", "common", "Items.d2o");
}
