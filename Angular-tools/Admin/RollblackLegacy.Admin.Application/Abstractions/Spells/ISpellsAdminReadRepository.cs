using RollblackLegacy.Admin.Application.Models.Spells;
using RollblackLegacy.Admin.Contracts.Spells;

namespace RollblackLegacy.Admin.Application.Abstractions.Spells;

public interface ISpellsAdminReadRepository
{
    Task<AdminPagedSpellsReadModel> SearchAsync(
        SpellCatalogSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminSpellDetailReadModel?> GetByIdAsync(
        short spellId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminSpellLevelDetailReadModel>?> GetLevelsAsync(
        short spellId,
        CancellationToken cancellationToken = default);

    Task<AdminSpellLevelDetailReadModel?> GetLevelAsync(
        short spellId,
        int levelNumber,
        CancellationToken cancellationToken = default);
}
