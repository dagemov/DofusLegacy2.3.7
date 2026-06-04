namespace ClientItemPublicationPipeline.Package;

internal static class ItemSkinCategoryMap
{
    private static readonly Dictionary<int, string> TypeIdToCategory = new()
    {
        [1] = "amuletos",
        [9] = "anillos",
        [10] = "cinturones",
        [11] = "botas",
        [15] = "recursos",
        [16] = "sombreros",
        [17] = "capas",
        [18] = "mascotas",
        [23] = "dofus",
        [82] = "escudos"
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
        "recursos"
    ];

    public static string ResolveCategory(int typeId) =>
        TypeIdToCategory.TryGetValue(typeId, out var category) ? category : "sin-categoria";

    public static bool IsSupportedExportCategory(string category) =>
        ExportCategories.Contains(category, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> PlannedAngularFolders { get; } = ExportCategories;
}
