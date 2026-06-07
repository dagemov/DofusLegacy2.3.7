namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed record SpellLevelUpdateResultDto(
    short SpellId,
    int LevelNumber,
    string WriteStrategy,
    SpellLevelDetailDto Level,
    IReadOnlyList<string> Warnings);
