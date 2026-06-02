namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemWriteResultDto(
    int ItemId,
    string Operation,
    string? ResolvedName,
    int DescriptionId,
    bool DescriptionPersisted,
    bool IsVisiblePersisted,
    string DetailPath,
    ItemPreviewStateDto PreviewState,
    IReadOnlyList<ItemWriteValidationProblem> Warnings);
