using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemSetsAdminReadService
{
    Task<ItemPagedResultDto<ItemSetListItemDto>> SearchAsync(ItemSetSearchRequest request, CancellationToken cancellationToken = default);

    Task<ItemSetDetailDto> GetByIdAsync(int setId, CancellationToken cancellationToken = default);
}
