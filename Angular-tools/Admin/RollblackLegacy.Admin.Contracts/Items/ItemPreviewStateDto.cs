namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemPreviewStateDto(
    string State,
    string ByItemPath,
    string ByIconPath,
    string ManualPath,
    string ByCategoryPath,
    string PreviewSource,
    string? ResolvedPath,
    string FallbackUsed);
