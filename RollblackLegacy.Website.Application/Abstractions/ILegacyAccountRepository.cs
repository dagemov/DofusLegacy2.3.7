using RollblackLegacy.Website.Domain.Accounts;

namespace RollblackLegacy.Website.Application.Abstractions;

public interface ILegacyAccountRepository
{
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<LegacyAccountSchemaCapabilities> CreateAsync(
        LegacyAccountRegistration registration,
        CancellationToken cancellationToken);
}
