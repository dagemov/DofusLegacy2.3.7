namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record AdminItemListReadModel(
    int ItemId,
    string? ResolvedName,
    int TypeId,
    string? TypeName,
    int Level,
    int? SetId,
    string? SetName,
    int IconId,
    int AppearanceId);
