namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemEffectEditRowRequest(
    string? RowId,
    short SerializationTypeId,
    int EffectId,
    int DiceNum,
    int DiceSide,
    int Value,
    int MinValue,
    int MaxValue,
    string? PreservedEffectHex);
