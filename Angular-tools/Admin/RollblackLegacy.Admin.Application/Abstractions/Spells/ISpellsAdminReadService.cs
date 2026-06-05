using RollblackLegacy.Admin.Contracts.Spells;

namespace RollblackLegacy.Admin.Application.Abstractions.Spells;

public interface ISpellsAdminReadService
{
    Task<SpellPagedResultDto<SpellCatalogItemDto>> SearchAsync(
        SpellCatalogSearchRequest request,
        CancellationToken cancellationToken = default);
}
