namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemDetailDto(
    int ItemId,
    string? ResolvedName,
    string? Description,
    int DescriptionId,
    int TypeId,
    string? TypeName,
    int Level,
    int Weight,
    double Price,
    bool Usable,
    bool Targetable,
    bool TwoHanded,
    bool Etheral,
    string? Criteria,
    int IconId,
    int AppearanceId,
    ItemSetLinkDto? Set,
    ItemClientIdentityDto ClientIdentity,
    ItemPreviewStateDto PreviewState,
    IReadOnlyList<ItemWarningDto> Warnings,
    IReadOnlyList<ItemEffectDto> Effects);
