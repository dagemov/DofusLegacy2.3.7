using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemSetsAdminReadService
{
    Task<IReadOnlyList<ItemSetListItemDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<ItemSetDetailDto> GetByIdAsync(int setId, CancellationToken cancellationToken = default);
}
