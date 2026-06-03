namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemAppearancePreviewStateDto(
    int AppearanceId,
    bool? AppearanceKnown,
    string State,
    string ByAppearancePath,
    string PreviewSource,
    string? ResolvedPath,
    string? AppearancesD2oPath);
