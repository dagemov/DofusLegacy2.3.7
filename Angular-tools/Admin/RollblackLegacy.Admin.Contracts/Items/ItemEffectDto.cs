namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemEffectDto(
    int EffectId,
    int DiceNum,
    int DiceSide,
    int Value,
    string Description);
