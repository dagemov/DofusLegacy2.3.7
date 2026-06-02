namespace RollblackLegacy.Admin.Application.Models.Items;

public sealed record ItemEffectEntryModel(
    short SerializationTypeId,
    int EffectId,
    int DiceNum,
    int DiceSide,
    int Value,
    int MinValue,
    int MaxValue,
    bool IsSupported,
    string? PreservedEffectHex);

public sealed record ItemEffectsDecodeModel(
    IReadOnlyList<ItemEffectEntryModel> Entries,
    string? PreservedSuffixHex,
    IReadOnlyList<string> Warnings);
