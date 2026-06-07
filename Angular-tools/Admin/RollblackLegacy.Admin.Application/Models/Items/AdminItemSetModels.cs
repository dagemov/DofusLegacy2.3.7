namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record AdminItemSetListReadModel(
    int SetId,
    string Name,
    int Level,
    int ItemCount,
    string EffectsHex,
    IReadOnlyList<int> PreviewIconIds);

public sealed record AdminPagedItemSetsReadModel(
    int TotalCount,
    IReadOnlyList<AdminItemSetListReadModel> Items);

public sealed record AdminItemSetDetailReadModel(
    int SetId,
    string Name,
    int Level,
    bool BonusIsSecret,
    string EffectsHex,
    IReadOnlyList<AdminItemSetMemberReadModel> Items);

public sealed record AdminItemSetMemberReadModel(
    int ItemId,
    string Name,
    int TypeId,
    string TypeName,
    int IconId,
    int AppearanceId,
    int Level);

public sealed record AdminItemSetWriteDraft(
    string Name,
    IReadOnlyList<int> ItemIds,
    string EffectsHex);
