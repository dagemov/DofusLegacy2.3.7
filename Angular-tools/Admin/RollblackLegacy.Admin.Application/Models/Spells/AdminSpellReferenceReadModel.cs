namespace RollblackLegacy.Admin.Application.Models.Spells;

public sealed record AdminSpellReferenceReadModel(
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
