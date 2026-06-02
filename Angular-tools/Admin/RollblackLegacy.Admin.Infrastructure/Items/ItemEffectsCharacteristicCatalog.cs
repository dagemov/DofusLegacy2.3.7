using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Infrastructure.Items;

public sealed class ItemEffectsCharacteristicCatalog : IItemEffectsCharacteristicCatalog
{
    private static readonly (int EffectId, string Label, string Group, short DefaultSerializationTypeId)[] SupportedCharacteristics =
    [
        (111, "AP / PA", "Combat stats", SunshineItemEffectsCodec.TypeInteger),
        (128, "MP / PM", "Combat stats", SunshineItemEffectsCodec.TypeInteger),
        (19, "MP / PM (alt)", "Combat stats", SunshineItemEffectsCodec.TypeInteger),
        (61, "Vitality", "Core stats", SunshineItemEffectsCodec.TypeInteger),
        (60, "Wisdom", "Core stats", SunshineItemEffectsCodec.TypeInteger),
        (54, "Strength", "Core stats", SunshineItemEffectsCodec.TypeInteger),
        (62, "Intelligence", "Core stats", SunshineItemEffectsCodec.TypeInteger),
        (59, "Chance", "Core stats", SunshineItemEffectsCodec.TypeInteger),
        (55, "Agility", "Core stats", SunshineItemEffectsCodec.TypeInteger),
        (118, "Damage", "Combat stats", SunshineItemEffectsCodec.TypeInteger),
        (53, "Range", "Combat stats", SunshineItemEffectsCodec.TypeInteger),
        (51, "Critical Hit", "Combat stats", SunshineItemEffectsCodec.TypeInteger),
        (114, "Summon", "Other", SunshineItemEffectsCodec.TypeInteger),
        (93, "Decrease Weight (Pods)", "Other", SunshineItemEffectsCodec.TypeInteger),
        (210, "Earth Resist %", "Resistances", SunshineItemEffectsCodec.TypeInteger),
        (211, "Water Resist %", "Resistances", SunshineItemEffectsCodec.TypeInteger),
        (212, "Air Resist %", "Resistances", SunshineItemEffectsCodec.TypeInteger),
        (213, "Fire Resist %", "Resistances", SunshineItemEffectsCodec.TypeInteger),
        (214, "Neutral Resist %", "Resistances", SunshineItemEffectsCodec.TypeInteger),
    ];

    private readonly IItemEffectNameResolver _effectNameResolver;

    public ItemEffectsCharacteristicCatalog(IItemEffectNameResolver effectNameResolver)
    {
        _effectNameResolver = effectNameResolver;
    }

    public IReadOnlyList<AdminEffectOptionDto> GetOptions()
    {
        return SupportedCharacteristics
            .Select(x => new AdminEffectOptionDto(
                x.EffectId,
                x.Label,
                _effectNameResolver.GetEffectName(x.EffectId),
                x.Group,
                x.DefaultSerializationTypeId,
                OperatorMode: "Integer",
                IsCharacteristic: true,
                IsSupported: true))
            .ToList();
    }

    public bool IsCharacteristic(int effectId) =>
        SupportedCharacteristics.Any(x => x.EffectId == effectId);

    public short GetDefaultSerializationTypeId(int effectId)
    {
        var match = SupportedCharacteristics.FirstOrDefault(x => x.EffectId == effectId);
        return match.EffectId == effectId ? match.DefaultSerializationTypeId : SunshineItemEffectsCodec.TypeInteger;
    }

    public string GetGroup(int effectId)
    {
        var match = SupportedCharacteristics.FirstOrDefault(x => x.EffectId == effectId);
        return match.EffectId == effectId ? match.Group : "Other / unsupported";
    }

    public string? GetCharacteristicLabel(int effectId)
    {
        var match = SupportedCharacteristics.FirstOrDefault(x => x.EffectId == effectId);
        return match.EffectId == effectId ? match.Label : null;
    }
}
