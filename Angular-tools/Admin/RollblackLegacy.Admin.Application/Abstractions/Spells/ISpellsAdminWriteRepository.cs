using RollblackLegacy.Admin.Application.Models.Spells;

namespace RollblackLegacy.Admin.Application.Abstractions.Spells;

public interface ISpellsAdminWriteRepository
{
    Task<AdminSpellLevelUpdateResultModel?> UpdateLevelAsync(
        short spellId,
        int levelNumber,
        AdminSpellLevelUpdateDraft draft,
        CancellationToken cancellationToken = default);
}
