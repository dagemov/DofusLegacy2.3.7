using RollblackLegacy.Admin.Application.Models.ClientIdentity;

namespace RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;

public interface IClientItemIdentityRepository
{
    Task<IReadOnlyList<ClientItemDbSnapshot>> GetItemsAsync(IReadOnlyList<int> itemIds, CancellationToken cancellationToken = default);
}
