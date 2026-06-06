namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemIconCategoryStatDto(
    string Category,
    string Label,
    int Count,
    DateTimeOffset? LastExtractionUtc,
    string PreviewSource);

public sealed record ItemIconCategoryStatsDto(
    int TotalPngInAngular,
    int TotalCataloged,
    int WeaponsExcluded,
    string PreviewSource,
    IReadOnlyList<ItemIconCategoryStatDto> Categories);
