namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed record SpellEffectRowDto(
    int RowIndex,
    int EffectId,
    string Label,
    string ProtocolName,
    string Group,
    string OperatorMode,
    int Value,
    int MinValue,
    int MaxValue,
    int? Delay,
    int? Random,
    int Duration,
    int TargetType,
    int ZoneShape,
    int ZoneMinSize,
    int ZoneSize,
    string PreviewText);
