namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemIconOptionDto(
    int IconId,
    string? PreviewPath,
    string PreviewState,
    string Source,
    bool HasPreview,
    int? LinkedItemCount,
    IReadOnlyList<string> SampleItemNames,
    string? Category = null,
    string? NameEs = null,
    string? NameEn = null,
    int? SampleItemId = null);
