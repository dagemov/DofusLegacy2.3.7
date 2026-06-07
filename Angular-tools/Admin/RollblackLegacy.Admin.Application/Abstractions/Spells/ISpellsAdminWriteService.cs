using RollblackLegacy.Admin.Contracts.Spells;

namespace RollblackLegacy.Admin.Application.Abstractions.Spells;

public interface ISpellsAdminWriteService
{
    Task<SpellLevelUpdateResultDto> UpdateLevelAsync(
        short spellId,
        int levelNumber,
        SpellLevelUpdateRequest request,
        CancellationToken cancellationToken = default);
}
