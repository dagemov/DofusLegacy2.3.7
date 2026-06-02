namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record ItemEffectEditDto(
    string RowId,
    short SerializationTypeId,
    int EffectId,
    string Label,
    int DiceNum,
    int DiceSide,
    int Value,
    int MinValue,
    int MaxValue,
    string OperatorMode,
    string Group,
    bool IsCharacteristic,
    bool IsSupported,
    string? Warning,
    string? PreservedEffectHex,
    string PreviewText);
