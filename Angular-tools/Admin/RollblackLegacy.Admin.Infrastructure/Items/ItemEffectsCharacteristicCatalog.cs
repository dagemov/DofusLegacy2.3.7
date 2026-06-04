using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Infrastructure.Items;

public sealed class ItemEffectsCharacteristicCatalog : IItemEffectsCharacteristicCatalog
{
    private readonly IItemEffectNameResolver _effectNameResolver;

    public ItemEffectsCharacteristicCatalog(IItemEffectNameResolver effectNameResolver)
    {
        _effectNameResolver = effectNameResolver;
    }

    public IReadOnlyList<AdminEffectOptionDto> GetOptions()
    {
        return LegacyBlazorEffectLabelRegistry.GetCharacteristicOptions()
            .Select(x => new AdminEffectOptionDto(
                x.EffectId,
                x.Metadata.Label,
                _effectNameResolver.GetEffectName(x.EffectId),
                x.Metadata.Group,
                x.Metadata.DefaultSerializationTypeId,
                x.Metadata.OperatorMode,
                x.Metadata.OperatorMode,
                x.Metadata.SortPriority,
                IsCharacteristic: true,
                IsSupported: true))
            .ToList();
    }

    public bool IsCharacteristic(int effectId) =>
        LegacyBlazorEffectLabelRegistry.TryGetCharacteristic(effectId, out _);

    public short GetDefaultSerializationTypeId(int effectId) =>
        LegacyBlazorEffectLabelRegistry.TryGetCharacteristic(effectId, out var metadata)
            ? metadata.DefaultSerializationTypeId
            : SunshineItemEffectsCodec.TypeInteger;

    public string GetGroup(int effectId) =>
        LegacyBlazorEffectLabelRegistry.ResolveGroup(
            effectId,
            _effectNameResolver.GetEffectName(effectId),
            GetCharacteristicLabel(effectId));

    public string? GetCharacteristicLabel(int effectId) =>
        LegacyBlazorEffectLabelRegistry.TryGetCharacteristic(effectId, out var metadata)
            ? metadata.Label
            : null;
}
