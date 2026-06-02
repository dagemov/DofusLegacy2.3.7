namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemIconOptionDto(
    int IconId,
    string? PreviewPath,
    string Source,
    bool HasPreview,
    int? LinkedItemCount,
    IReadOnlyList<string> SampleItemNames);
