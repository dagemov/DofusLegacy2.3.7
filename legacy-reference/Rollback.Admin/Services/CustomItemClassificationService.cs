using Rollback.World.CustomEnums;

namespace Rollback.Admin.Services;

public sealed class CustomItemClassificationService
{
    private readonly ReferenceItemCatalogService _referenceItemCatalogService;

    public CustomItemClassificationService(ReferenceItemCatalogService referenceItemCatalogService) =>
        _referenceItemCatalogService = referenceItemCatalogService;

    public bool IsCustomItem(short itemId, short? itemSetId, string? displayName, string? setName)
    {
        if (ContainsRollBackToken(displayName) || ContainsRollBackToken(setName))
            return true;

        var referenceItem = _referenceItemCatalogService.GetItem(itemId);
        if (referenceItem is null)
            return true;

        if (itemSetId is > 0 && _referenceItemCatalogService.GetSet(itemSetId.Value) is null)
            return true;

        return false;
    }

    public bool IsRollBackVendorEligible(short itemId, short? itemSetId, ItemType type, string? displayName, string? setName) =>
        NpcVendorCatalogService.IsRollBackSupportedType(type) &&
        IsCustomItem(itemId, itemSetId, displayName, setName);

    private static bool ContainsRollBackToken(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains("rollback", StringComparison.OrdinalIgnoreCase);
}
