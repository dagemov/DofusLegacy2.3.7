using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemsAdminWriteService
{
    Task<ItemWriteResultDto> CreateAsync(ItemCreateRequest request, CancellationToken cancellationToken = default);

    Task<ItemWriteResultDto> UpdateAsync(int itemId, ItemUpdateRequest request, CancellationToken cancellationToken = default);

    Task<ItemWriteResultDto> DuplicateAsync(int sourceItemId, ItemDuplicateRequest request, CancellationToken cancellationToken = default);

    Task<ItemPreviewStateDto> ResolvePreviewStateAsync(int? itemId, int? iconId, CancellationToken cancellationToken = default);
}
