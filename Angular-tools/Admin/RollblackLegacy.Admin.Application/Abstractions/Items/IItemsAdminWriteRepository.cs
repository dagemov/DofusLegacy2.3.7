using RollblackLegacy.Admin.Application.Models.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemsAdminWriteRepository
{
    Task<AdminItemWriteRow?> GetByIdAsync(int itemId, CancellationToken cancellationToken = default);

    Task<bool> ItemSetExistsAsync(int itemSetId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<int>> GetWeaponTypeIdsAsync(CancellationToken cancellationToken = default);

    Task<AdminItemWriteRow> CreateAsync(AdminItemWriteDraft draft, CancellationToken cancellationToken = default);

    Task<AdminItemWriteRow?> UpdateAsync(int itemId, AdminItemWriteDraft draft, CancellationToken cancellationToken = default);

    Task<AdminItemWriteRow?> DuplicateAsync(int sourceItemId, AdminItemWriteDraft draft, CancellationToken cancellationToken = default);
}
