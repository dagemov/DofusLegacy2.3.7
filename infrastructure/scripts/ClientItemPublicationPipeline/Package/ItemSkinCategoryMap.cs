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

    public static string ResolveCategory(int typeId, int itemSetId)
    {
        if (itemSetId > 0)
        {
            return "sets";
        }

        return TypeIdToCategory.TryGetValue(typeId, out var category)
            ? category
            : "sin-categoria";
    }

    public static IReadOnlyList<string> PlannedAngularFolders { get; } =
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
        "sets",
        "recursos",
        "sin-categoria"
    ];
}
