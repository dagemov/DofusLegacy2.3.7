namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record AdminItemEffectReadModel(
    int EffectId,
    int DiceNum,
    int DiceSide,
    int Value,
    string Description);
