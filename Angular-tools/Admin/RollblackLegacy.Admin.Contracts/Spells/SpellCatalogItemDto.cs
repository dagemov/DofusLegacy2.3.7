namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed record SpellCatalogItemDto(
    short SpellId,
    string? Name,
    string? Description,
    int? TypeId,
    string? TypeLabel,
    int? IconId,
    IReadOnlyList<SpellBreedSummaryDto> Breeds,
    int LevelCount,
    bool RuntimeAvailable,
    bool ReferenceAvailable);
