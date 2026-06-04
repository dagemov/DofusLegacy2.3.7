namespace ClientItemPublicationPipeline.Package;

/// <summary>
/// TypeIds de armas alineados con ItemsAdminWriteService.UnsupportedWeaponTypeIds (sin DB).
/// </summary>
internal static class WeaponTypeFilter
{
    public static readonly HashSet<int> WeaponTypeIds =
    [
        2, 3, 4, 5, 6, 7, 8, 19, 20, 21, 22, 83, 99, 102, 114
    ];

    public static bool IsWeapon(int typeId) => WeaponTypeIds.Contains(typeId);

    public static bool IsWeapon(int typeId, ItemTypeWeaponRegistry registry) => registry.IsWeapon(typeId);

    public static bool ExcludeWeapons(string? excludeTypes) =>
        string.IsNullOrWhiteSpace(excludeTypes) ||
        excludeTypes.Contains("weapon", StringComparison.OrdinalIgnoreCase) ||
        excludeTypes.Contains("weapons", StringComparison.OrdinalIgnoreCase) ||
        excludeTypes.Contains("armas", StringComparison.OrdinalIgnoreCase);
}
