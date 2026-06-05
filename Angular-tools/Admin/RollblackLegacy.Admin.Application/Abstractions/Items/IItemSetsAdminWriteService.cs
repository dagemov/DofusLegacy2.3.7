using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemSetsAdminWriteService
{
    Task<ItemSetWriteResultDto> CreateAsync(ItemSetCreateRequest request, CancellationToken cancellationToken = default);

    Task<ItemSetWriteResultDto> UpdateAsync(int setId, ItemSetUpdateRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int setId, CancellationToken cancellationToken = default);
}
