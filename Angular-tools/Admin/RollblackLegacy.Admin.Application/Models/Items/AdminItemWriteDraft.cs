namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record AdminItemWriteDraft(
    string ResolvedName,
    string? Description,
    int TypeId,
    int Level,
    int Weight,
    double Price,
    int IconId,
    int AppearanceId,
    int? SetId,
    string Conditions,
    bool? IsVisible,
    bool Usable,
    bool Targetable,
    bool TwoHanded,
    bool Etheral,
    string? EffectsHex = null);
