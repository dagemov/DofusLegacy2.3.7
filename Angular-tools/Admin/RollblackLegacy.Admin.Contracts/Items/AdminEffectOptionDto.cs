namespace RollblackLegacy.Admin.Contracts.Items;

public sealed record AdminEffectOptionDto(
    int EffectId,
    string Label,
    string ProtocolName,
    string Group,
    short DefaultSerializationTypeId,
    string OperatorMode,
    bool IsCharacteristic,
    bool IsSupported);
