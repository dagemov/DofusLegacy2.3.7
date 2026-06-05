using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemEffectsCatalog
{
    IReadOnlyList<AdminEffectOptionDto> GetOptions();
}
