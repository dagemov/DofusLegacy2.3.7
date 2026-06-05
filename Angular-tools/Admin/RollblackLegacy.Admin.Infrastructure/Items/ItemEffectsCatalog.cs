using RollblackLegacy.Admin.Application.Abstractions.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Infrastructure.Items;

public sealed class ItemEffectsCatalog : IItemEffectsCatalog
{
    private readonly EffectsEnumCatalogReader _effectsEnumCatalogReader;
    private readonly IItemEffectNameResolver _effectNameResolver;
    private readonly IItemEffectsCharacteristicCatalog _characteristicCatalog;
    private readonly Lazy<IReadOnlyList<AdminEffectOptionDto>> _options;

    public ItemEffectsCatalog(
        EffectsEnumCatalogReader effectsEnumCatalogReader,
        IItemEffectNameResolver effectNameResolver,
        IItemEffectsCharacteristicCatalog characteristicCatalog)
    {
        _effectsEnumCatalogReader = effectsEnumCatalogReader;
        _effectNameResolver = effectNameResolver;
        _characteristicCatalog = characteristicCatalog;
        _options = new Lazy<IReadOnlyList<AdminEffectOptionDto>>(BuildOptions);
    }

    public IReadOnlyList<AdminEffectOptionDto> GetOptions() => _options.Value;

    private IReadOnlyList<AdminEffectOptionDto> BuildOptions()
    {
        return _effectsEnumCatalogReader
            .GetEffectIds()
            .Distinct()
            .Select(BuildOption)
            .OrderBy(x => x.SortPriority)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private AdminEffectOptionDto BuildOption(int effectId)
    {
        var protocolName = _effectNameResolver.GetEffectName(effectId);
        var label = ItemEffectDisplayMetadata.GetDisplayLabel(effectId, protocolName);
        var format = ItemEffectDisplayMetadata.SuggestFormat(effectId, protocolName);
        var group = ItemEffectDisplayMetadata.ResolveGroup(effectId, protocolName, label);
        var isCharacteristic = _characteristicCatalog.IsCharacteristic(effectId);
        var defaultSerializationTypeId = isCharacteristic
            ? _characteristicCatalog.GetDefaultSerializationTypeId(effectId)
            : ItemEffectDisplayMetadata.MapFormatToSerializationTypeId(format);

        return new AdminEffectOptionDto(
            effectId,
            label,
            protocolName,
            group,
            defaultSerializationTypeId,
            format,
            format,
            ItemEffectDisplayMetadata.GetSortPriority(effectId, label),
            isCharacteristic,
            IsSupported: true);
    }
}
