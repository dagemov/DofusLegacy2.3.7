using RollblackLegacy.Admin.Contracts.Common;
using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemsAdminReadService
{
    Task<ItemPagedResultDto<ItemListItemDto>> SearchAsync(ItemSearchRequest request, CancellationToken cancellationToken = default);

    Task<ItemPagedResultDto<ItemIconOptionDto>> SearchIconsAsync(ItemIconSearchRequest request, CancellationToken cancellationToken = default);

    Task<ItemDetailDto> GetItemAsync(int itemId, CancellationToken cancellationToken = default);

    Task<ItemClientIdentityDto> GetIdentityAsync(int itemId, CancellationToken cancellationToken = default);

    Task<ItemQaSummaryDto> GetQaSummaryAsync(int itemId, CancellationToken cancellationToken = default);

    Task<ItemPublicationStatusDto> GetPublicationStatusAsync(int itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminOptionDto>> GetTypeOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminOptionDto>> GetItemSetOptionsAsync(CancellationToken cancellationToken = default);
}
