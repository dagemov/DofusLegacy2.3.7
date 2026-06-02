namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemEffectsEditDto(
    int ItemId,
    string EffectsHex,
    IReadOnlyList<ItemEffectEditDto> Effects,
    string? PreservedSuffixHex,
    IReadOnlyList<string> Warnings,
    bool HasUnsupportedEffects);
