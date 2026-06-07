namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed record SpellLevelEffectsDto(
    short SpellId,
    int LevelNumber,
    SpellEffectCollectionDto Effects,
    SpellEffectCollectionDto CriticalEffects);
