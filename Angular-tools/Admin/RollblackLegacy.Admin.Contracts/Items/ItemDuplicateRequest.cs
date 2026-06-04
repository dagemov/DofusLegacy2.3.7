namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemDuplicateRequest(
    string? ResolvedName,
    string? Description,
    int TypeId,
    int Level,
    int Weight,
    double Price,
    int IconId,
    int AppearanceId,
    int? SetId,
    string? Conditions,
    bool? IsVisible,
    bool Usable,
    bool Targetable,
    bool TwoHanded,
    bool Etheral);
