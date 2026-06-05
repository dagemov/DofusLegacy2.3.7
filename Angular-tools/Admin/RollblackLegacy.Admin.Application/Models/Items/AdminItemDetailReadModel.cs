namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record AdminItemDetailReadModel(
    int ItemId,
    string? ResolvedName,
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
    int? SetId,
    string? SetName,
    IReadOnlyList<AdminItemEffectReadModel> Effects);
