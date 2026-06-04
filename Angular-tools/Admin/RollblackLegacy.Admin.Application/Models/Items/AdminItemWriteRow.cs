namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record AdminItemWriteRow(
    int ItemId,
    string ResolvedName,
    int DescriptionId,
    int TypeId,
    int Level,
    int Weight,
    double Price,
    bool Usable,
    bool Targetable,
    bool TwoHanded,
    bool Etheral,
    string Conditions,
    int IconId,
    int AppearanceId,
    int? SetId);
