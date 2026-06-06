namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record AdminItemSetListReadModel(
    int SetId,
    string Name,
    int ItemCount,
    string EffectsHex);

public sealed record AdminItemSetDetailReadModel(
    int SetId,
    string Name,
    bool BonusIsSecret,
    string EffectsHex,
    IReadOnlyList<AdminItemSetMemberReadModel> Items);

public sealed record AdminItemSetMemberReadModel(
    int ItemId,
    string Name,
    int TypeId,
    string TypeName,
    int IconId,
    int AppearanceId);
