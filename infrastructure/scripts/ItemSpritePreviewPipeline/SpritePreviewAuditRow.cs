namespace ItemSpritePreviewPipeline;

internal sealed record SpritePreviewAuditRow(
    int ItemId,
    string? DbName,
    int? IconId,
    int? AppearanceId,
    bool ClientKnown,
    bool IconPreviewAvailable,
    bool AppearancePreviewAvailable,
    string? CuratedIconSourceFile,
    string? CuratedAppearanceSourceFile,
    string ClientAssetSourceHint,
    bool RequiresClientPatch,
    bool CanResolveAutomatically,
    string RecommendedNextStep);
