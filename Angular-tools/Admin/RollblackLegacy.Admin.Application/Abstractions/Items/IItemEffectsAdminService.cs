using RollblackLegacy.Admin.Contracts.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemEffectsAdminService
{
    Task<ItemEffectsEditDto> GetEditAsync(int itemId, CancellationToken cancellationToken = default);

    Task<ItemEffectsUpdateResultDto> UpdateAsync(
        int itemId,
        ItemEffectsUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminEffectOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);
}
