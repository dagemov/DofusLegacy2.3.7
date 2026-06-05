using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Application.Models.Items;

namespace RollblackLegacy.Admin.Infrastructure.Items;

public sealed class ItemEffectsCodecAdapter : IItemEffectsCodec
{
    private readonly SunshineItemEffectsCodec _codec = new();

    public string EmptyEffectsHex => SunshineItemEffectsCodec.EmptyEffectsHex;

    public ItemEffectsDecodeModel Decode(string? hex)
    {
        var decoded = _codec.Decode(hex);
        return new ItemEffectsDecodeModel(
            decoded.Entries
                .Select(x => new ItemEffectEntryModel(
                    x.SerializationTypeId,
                    x.EffectId,
                    x.DiceNum,
                    x.DiceSide,
                    x.Value,
                    x.MinValue,
                    x.MaxValue,
                    x.IsSupported,
                    x.PreservedEffectHex))
                .ToList(),
            decoded.PreservedSuffixHex,
            decoded.Warnings);
    }

    public string Encode(IReadOnlyList<ItemEffectEntryModel> effects, string? preservedSuffixHex)
    {
        var entries = effects
            .Select(x => new SunshineEffectEntry(
                x.SerializationTypeId,
                x.EffectId,
                x.DiceNum,
                x.DiceSide,
                x.Value,
                x.MinValue,
                x.MaxValue,
                x.IsSupported,
                x.PreservedEffectHex))
            .ToList();

        return _codec.Encode(entries, preservedSuffixHex);
    }
}
