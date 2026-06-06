using RollblackLegacy.Admin.Contracts.Common;
using RollblackLegacy.Admin.Contracts.Items;
using RollblackLegacy.Admin.Application.Models.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemsAdminReadRepository
{
    Task<AdminPagedItemsReadModel> SearchAsync(ItemSearchRequest request, CancellationToken cancellationToken = default);

    Task<ItemPagedResultDto<ItemIconOptionDto>> SearchIconsAsync(ItemIconSearchRequest request, CancellationToken cancellationToken = default);

    Task<ItemIconCategoryStatsDto> GetIconCategoryStatsAsync(CancellationToken cancellationToken = default);

    Task<AdminItemDetailReadModel?> GetByIdAsync(int itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminOptionDto>> GetTypeOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminOptionDto>> GetItemSetOptionsAsync(CancellationToken cancellationToken = default);
}
