using RollblackLegacy.Admin.Application.Models.Items;

namespace RollblackLegacy.Admin.Application.Abstractions.Items;

public interface IItemSetsAdminReadRepository
{
    Task<IReadOnlyList<AdminItemSetListReadModel>> ListAsync(CancellationToken cancellationToken = default);

    Task<AdminItemSetDetailReadModel?> GetByIdAsync(int setId, CancellationToken cancellationToken = default);
}
