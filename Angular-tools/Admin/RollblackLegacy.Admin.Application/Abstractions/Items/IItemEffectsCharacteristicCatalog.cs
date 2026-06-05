using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemEffectsCharacteristicCatalog
{
    IReadOnlyList<AdminEffectOptionDto> GetOptions();

    bool IsCharacteristic(int effectId);

    short GetDefaultSerializationTypeId(int effectId);

    string GetGroup(int effectId);

    string? GetCharacteristicLabel(int effectId);
}
