namespace RollblackLegacy.Admin.Application.Models.Spells;

public sealed record AdminSpellDetailReadModel(
    short SpellId,
    string? Name,
    string? Description,
    int? TypeId,
    string? TypeLabel,
    int? IconId,
    IReadOnlyList<AdminSpellBreedReadModel> Breeds,
    int LevelCount,
    bool RuntimeAvailable,
    bool ReferenceAvailable,
    AdminSpellReferenceReadModel? Reference,
    IReadOnlyList<AdminSpellLevelSummaryReadModel> Levels);
