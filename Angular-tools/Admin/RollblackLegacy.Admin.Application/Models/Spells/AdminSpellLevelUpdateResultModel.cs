namespace RollblackLegacy.Admin.Application.Models.Spells;

public sealed record AdminSpellLevelUpdateResultModel(
    short SpellId,
    int LevelNumber,
    string WriteStrategy,
    IReadOnlyList<string> Warnings);
