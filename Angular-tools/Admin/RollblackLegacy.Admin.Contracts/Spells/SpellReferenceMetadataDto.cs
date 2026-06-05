namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed record SpellReferenceMetadataDto(
    string SourceDescription,
    string? Name,
    string? Description,
    int? NameId,
    int? DescriptionId,
    int TypeId,
    string? TypeLabel,
    int? IconId,
    IReadOnlyList<int> BreedIds,
    int LevelCount);
