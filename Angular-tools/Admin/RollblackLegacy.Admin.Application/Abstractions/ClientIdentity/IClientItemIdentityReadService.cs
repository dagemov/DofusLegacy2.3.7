using RollblackLegacy.Admin.Contracts.ClientIdentity;

namespace RollblackLegacy.Admin.Application.Abstractions.ClientIdentity;

public interface IClientItemIdentityReadService
{
    Task<ClientItemIdentityCheckResultDto> GetItemAsync(int itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientItemIdentityCheckResultDto>> CheckAsync(ClientItemIdentityCheckRequest request, CancellationToken cancellationToken = default);
}
