namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemQaSummaryDto(
    int ItemId,
    string? ResolvedName,
    string? Type,
    int Level,
    int IconId,
    int AppearanceId,
    ItemPreviewStateDto PreviewState,
    IReadOnlyList<ItemWarningDto> Warnings,
    string WorkflowState,
    bool CanQa,
    bool CanPublish,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RecommendedChecks);
