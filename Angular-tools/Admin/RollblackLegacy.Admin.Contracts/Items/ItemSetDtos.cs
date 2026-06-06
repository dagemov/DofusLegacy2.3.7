namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemSetListItemDto(
    int SetId,
    string Name,
    int ItemCount,
    int BonusTierCount);

public sealed record ItemSetBonusEffectDto(
    int EffectId,
    string Label,
    string ProtocolName,
    int Value,
    string Format);

public sealed record ItemSetBonusTierDto(
    int PieceCount,
    string TierLabel,
    IReadOnlyList<ItemSetBonusEffectDto> Effects);

public sealed record ItemSetMemberDto(
    int ItemId,
    string Name,
    int TypeId,
    string TypeName,
    int IconId,
    ItemPreviewStateDto PreviewState,
    string? PublicationSummary);

public sealed record ItemSetDetailDto(
    int SetId,
    string Name,
    bool BonusIsSecret,
    IReadOnlyList<ItemSetMemberDto> Items,
    IReadOnlyList<ItemSetBonusTierDto> BonusTiers);
