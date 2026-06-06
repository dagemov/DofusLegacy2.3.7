namespace ClientItemPublicationPipeline.Package;

internal static class ItemSkinCategoryMap
{
    private static readonly Dictionary<int, string> TypeIdToCategory = new()
    {
        [1] = "amuletos",
        [9] = "anillos",
        [10] = "cinturones",
        [11] = "botas",
        [12] = "consumibles",
        [15] = "recursos",
        [16] = "sombreros",
        [17] = "capas",
        [18] = "mascotas",
        [23] = "dofus",
        [28] = "consumibles",
        [33] = "consumibles",
        [42] = "consumibles",
        [43] = "consumibles",
        [69] = "consumibles",
        [79] = "consumibles",
        [82] = "escudos",
        [151] = "trofeos"
    };

    public static IReadOnlyList<string> ExportCategories { get; } =
    [
        "dofus",
        "sombreros",
        "capas",
        "botas",
        "mascotas",
        "escudos",
        "anillos",
        "amuletos",
        "cinturones",
        "recursos",
        "trofeos",
        "consumibles"
    ];

    public static string ResolveCategory(int typeId) =>
        TypeIdToCategory.TryGetValue(typeId, out var category) ? category : "sin-categoria";

    public static bool IsSupportedExportCategory(string category) =>
        ExportCategories.Contains(category, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> PlannedAngularFolders { get; } = ExportCategories;
}
