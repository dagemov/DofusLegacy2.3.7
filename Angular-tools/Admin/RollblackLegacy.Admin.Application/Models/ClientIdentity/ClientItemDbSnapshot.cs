namespace RollblackLegacy.Admin.Application.Models.ClientIdentity;

public sealed record ClientItemDbSnapshot(
    int ItemId,
    string? Name,
    int DescriptionId,
    int TypeId,
    int Level,
    int IconId,
    int AppearanceId,
    int ItemSetId);
