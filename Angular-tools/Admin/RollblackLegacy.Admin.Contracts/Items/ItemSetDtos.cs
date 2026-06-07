namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemSetListItemDto(
    int SetId,
    string Name,
    int Level,
    int ItemCount,
    int BonusTierCount,
    IReadOnlyList<string> PreviewItemIcons);

public sealed record ItemSetBonusEffectDto(
    int EffectId,
    string Label,
    string ProtocolName,
    int Value,
    int? DiceNum,
    int? DiceSide,
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
    string? PreviewPath,
    string? PublicationSummary);

public sealed record ItemSetDetailDto(
    int SetId,
    string Name,
    int Level,
    bool BonusIsSecret,
    IReadOnlyList<ItemSetMemberDto> Items,
    IReadOnlyList<ItemSetBonusTierDto> BonusTiers);
