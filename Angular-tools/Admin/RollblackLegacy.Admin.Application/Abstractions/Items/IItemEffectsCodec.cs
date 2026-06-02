using RollblackLegacy.Admin.Application.Models.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemEffectsCodec
{
    string EmptyEffectsHex { get; }

    ItemEffectsDecodeModel Decode(string? hex);

    string Encode(IReadOnlyList<ItemEffectEntryModel> effects, string? preservedSuffixHex);
}
