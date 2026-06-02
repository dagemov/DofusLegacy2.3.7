namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemListItemDto(
    int ItemId,
    string? ResolvedName,
    int TypeId,
    string? TypeName,
    int Level,
    int? SetId,
    string? SetName,
    int IconId,
    int AppearanceId,
    ItemPreviewStateDto PreviewState,
    int WarningCount);
