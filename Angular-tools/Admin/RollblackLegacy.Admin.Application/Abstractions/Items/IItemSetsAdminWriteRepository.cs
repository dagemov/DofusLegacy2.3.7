using RollblackLegacy.Admin.Application.Models.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemSetsAdminWriteRepository
{
    Task<int> CreateAsync(AdminItemSetWriteDraft draft, CancellationToken cancellationToken = default);

    Task UpdateAsync(int setId, AdminItemSetWriteDraft draft, CancellationToken cancellationToken = default);

    Task DeleteAsync(int setId, CancellationToken cancellationToken = default);
}
