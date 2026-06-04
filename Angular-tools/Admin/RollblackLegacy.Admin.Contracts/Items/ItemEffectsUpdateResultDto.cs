namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemEffectsUpdateResultDto(
    int ItemId,
    string EffectsHex,
    IReadOnlyList<ItemEffectEditDto> Effects,
    IReadOnlyList<string> Warnings);
