namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemPublicationStatusDto(
    int ItemId,
    string? ResolvedName,
    int IconId,
    int AppearanceId,
    ItemPreviewStateDto PreviewState,
    string VisibilityState,
    string ClientTemplateState,
    string PublicationState,
    bool ClientKnown,
    bool Published,
    bool NeedsClientPatch,
    bool NeedsAsset,
    bool NeedsQa,
    string? ClientRootPath,
    string? ItemsD2oPath,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> RecommendedActions);
