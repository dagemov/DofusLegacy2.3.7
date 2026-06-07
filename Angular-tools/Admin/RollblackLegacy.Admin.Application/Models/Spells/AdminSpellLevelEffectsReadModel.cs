namespace RollblackLegacy.Admin.Application.Models.Spells;

public sealed record AdminSpellLevelEffectsReadModel(
    short SpellId,
    int LevelNumber,
    AdminSpellEffectCollectionReadModel Effects,
    AdminSpellEffectCollectionReadModel CriticalEffects);
