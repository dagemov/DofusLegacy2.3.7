namespace ClientItemPublicationPipeline.Package;

internal static class PublicationPackagePatchFiles
{
    public static readonly string[] CoreRelativeFiles =
    [
        PublicationPackagePaths.ItemsRelative,
        PublicationPackagePaths.I18nEsRelative,
        PublicationPackagePaths.I18nEnRelative
    ];

    public static IReadOnlyList<string> ResolveRelativeFiles(string packageDirectory)
    {
        var files = new List<string>
        {
            PublicationPackagePaths.ItemsRelative,
            PublicationPackagePaths.I18nEsRelative,
            PublicationPackagePaths.I18nEnRelative
        };

        if (PublicationPackagePaths.TryResolveItemSetsPath(packageDirectory) is not null)
        {
            files.Add(PublicationPackagePaths.ItemSetsRelative);
        }

        return files;
    }

    public static IReadOnlyList<string> ResolveClientRelativeFiles(string clientRoot)
    {
        var files = new List<string>(CoreRelativeFiles);
        var itemSets = Path.Combine(clientRoot, PublicationPackagePaths.ItemSetsRelative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(itemSets))
        {
            files.Add(PublicationPackagePaths.ItemSetsRelative);
        }

        return files;
    }

    public static string ResolvePackageSourcePath(string packageDirectory, string relative)
    {
        return relative switch
        {
            _ when relative == PublicationPackagePaths.ItemsRelative =>
                PublicationPackagePaths.ResolveItemsPath(packageDirectory),
            _ when relative == PublicationPackagePaths.I18nEsRelative =>
                PublicationPackagePaths.ResolveI18nEsPath(packageDirectory),
            _ when relative == PublicationPackagePaths.I18nEnRelative =>
                PublicationPackagePaths.ResolveI18nEnPath(packageDirectory),
            _ when relative == PublicationPackagePaths.ItemSetsRelative =>
                PublicationPackagePaths.TryResolveItemSetsPath(packageDirectory)
                    ?? throw new FileNotFoundException("ItemSets.d2o no encontrado en el paquete."),
            _ => throw new InvalidOperationException($"Archivo no mapeado: {relative}")
        };
    }
}
