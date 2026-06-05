using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class NpcVendorCatalogService
{
    public const int RollBackShopActionId = 50;
    public const short RollBackNpcId = 809;

    public sealed record VendorDefinition(
        int ShopActionId,
        short NpcId,
        string DisplayName,
        ItemType[] Types,
        short? MinLevel = null,
        short? MaxLevel = null,
        string? Label = null,
        bool IsRollBackSpecial = false);

    private static readonly ItemType[] RollBackSupportedTypes =
    {
        ItemType.Chapeau,
        ItemType.Bouclier,
        ItemType.Cape,
        ItemType.Bottes,
        ItemType.Ceinture,
        ItemType.Amulette,
        ItemType.Anneau,
        ItemType.Familier,
        ItemType.CertificatDeDragodinde,
    };

    private static readonly VendorDefinition[] Definitions =
    {
        new(12560, 560, "Vendedora de sombreros", new[] { ItemType.Chapeau }),
        new(12571, 571, "Vendedora de capas", new[] { ItemType.Cape }),
        new(12552, 552, "Comerciante de cinturones", new[] { ItemType.Ceinture }),
        new(12573, 573, "Comerciante de botas", new[] { ItemType.Bottes }),
        new(56, 816, "Mercader de arcos y arbalestas", new[] { ItemType.Arc, ItemType.Arbalete }, Label: "Arcos y arbalestas"),
        new(12547, 1236, "Herrero de filo y golpe (1-100)", new[] { ItemType.Dague, ItemType.Epee, ItemType.Hache, ItemType.Faux }, MaxLevel: 100, Label: "Espadas, dagas, hachas y hoces 1-100"),
        new(12574, 1237, "Herrero de filo y golpe (101-200)", new[] { ItemType.Dague, ItemType.Epee, ItemType.Hache, ItemType.Faux }, MinLevel: 101, Label: "Espadas, dagas, hachas y hoces 101-200"),
        new(12612, 1240, "Escultor de varitas y bastones (1-100)", new[] { ItemType.Baguette, ItemType.Baton }, MaxLevel: 100, Label: "Varitas y bastones 1-100"),
        new(12613, 1241, "Escultor de varitas y bastones (101-200)", new[] { ItemType.Baguette, ItemType.Baton }, MinLevel: 101, Label: "Varitas y bastones 101-200"),
        new(51, 1053, "Mercader de dofus", new[] { ItemType.Dofus }),
        new(12640, 1252, "Mercader de combustibles", new[] { ItemType.ParcheminDExperience, ItemType.ParcheminDeSort, ItemType.ParcheminDeCaracteristique, ItemType.ObjetVivant }, Label: "Pergaminos, spell scrolls y objevivos"),
        new(61, 792, "Mercader de escudos", new[] { ItemType.Bouclier }),
        new(60, 791, "Cuidadora de mascotas RollBack", new[] { ItemType.Familier, ItemType.CertificatDeDragodinde }),
        new(62, 793, "Vendedora de amuletos", new[] { ItemType.Amulette }),
        new(63, 794, "Comerciante de anillos", new[] { ItemType.Anneau }),
        new(52, 790, "Mercader de runas", new[] { ItemType.RuneDeForgemagie }),
        new(RollBackShopActionId, RollBackNpcId, "Mercader de Sets RollBack", RollBackSupportedTypes, Label: "Sets RollBack", IsRollBackSpecial: true),
    };

    private readonly IReadOnlyDictionary<int, VendorDefinition> _definitionsByShopActionId = Definitions
        .ToDictionary(definition => definition.ShopActionId);

    private readonly IReadOnlyDictionary<short, string> _namesByNpcId = Definitions
        .GroupBy(definition => definition.NpcId)
        .ToDictionary(group => group.Key, group => group.First().DisplayName);

    public bool IsRollBackVendor(int shopActionId) =>
        shopActionId == RollBackShopActionId;

    public bool SupportsRollBackType(ItemType type) =>
        IsRollBackSupportedType(type);

    public static bool IsRollBackSupportedType(ItemType type) =>
        RollBackSupportedTypes.Contains(type);

    public bool IsCompatible(int shopActionId, ItemType type, short level, bool isRollBackItem = false)
    {
        if (!_definitionsByShopActionId.TryGetValue(shopActionId, out var definition))
            return true;

        if (!definition.Types.Contains(type))
            return false;

        if (definition.IsRollBackSpecial && !isRollBackItem)
            return false;

        if (definition.MinLevel.HasValue && level < definition.MinLevel.Value)
            return false;

        if (definition.MaxLevel.HasValue && level > definition.MaxLevel.Value)
            return false;

        return true;
    }

    public int? ResolvePreferredShopActionId(ItemType type, short level)
    {
        var definition = Definitions.FirstOrDefault(candidate =>
            !candidate.IsRollBackSpecial &&
            candidate.Types.Contains(type) &&
            (!candidate.MinLevel.HasValue || level >= candidate.MinLevel.Value) &&
            (!candidate.MaxLevel.HasValue || level <= candidate.MaxLevel.Value));

        return definition?.ShopActionId;
    }

    public VendorDefinition? GetDefinition(int shopActionId) =>
        _definitionsByShopActionId.TryGetValue(shopActionId, out var definition)
            ? definition
            : null;

    public string ResolveVendorName(short npcId, string? runtimeName, string categoryLabel)
    {
        if (_namesByNpcId.TryGetValue(npcId, out var knownName))
            return knownName;

        if (!string.IsNullOrWhiteSpace(runtimeName))
            return runtimeName.Trim();

        if (!string.IsNullOrWhiteSpace(categoryLabel))
            return $"Vendor de {categoryLabel}";

        return $"NPC {npcId}";
    }

    public IReadOnlyDictionary<int, string> GetSuggestedCatalogLabels() =>
        Definitions.ToDictionary(
            definition => definition.ShopActionId,
            definition => definition.Label ?? string.Join(", ", definition.Types.Select(ItemTypeLabelService.GetDisplayName)));

    public IReadOnlyList<ItemType> GetSuggestedTypes(int shopActionId) =>
        _definitionsByShopActionId.TryGetValue(shopActionId, out var definition)
            ? definition.Types
            : Array.Empty<ItemType>();

    public IReadOnlyList<ItemType> GetSupportedFilterTypes(int shopActionId)
    {
        if (_definitionsByShopActionId.TryGetValue(shopActionId, out var definition))
            return definition.Types.Distinct().OrderBy(type => (short)type).ToArray();

        return Array.Empty<ItemType>();
    }
}
