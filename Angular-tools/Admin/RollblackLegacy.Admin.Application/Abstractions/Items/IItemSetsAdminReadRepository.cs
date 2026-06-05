using RollblackLegacy.Admin.Application.Models.Items;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemSetsAdminReadRepository
{
    Task<AdminPagedItemSetsReadModel> SearchAsync(ItemSetSearchRequest request, CancellationToken cancellationToken = default);

    Task<AdminItemSetDetailReadModel?> GetByIdAsync(int setId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int setId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ResolveExistingItemIdsAsync(IReadOnlyList<int> itemIds, CancellationToken cancellationToken = default);
}
