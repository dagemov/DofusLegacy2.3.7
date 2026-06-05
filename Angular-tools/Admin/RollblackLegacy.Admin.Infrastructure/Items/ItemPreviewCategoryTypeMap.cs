namespace RollblackLegacy.Admin.Infrastructure.Items;

internal static class ItemPreviewCategoryTypeMap
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

    public static string? ResolveCategory(int typeId) =>
        TypeIdToCategory.TryGetValue(typeId, out var category) ? category : null;
}
