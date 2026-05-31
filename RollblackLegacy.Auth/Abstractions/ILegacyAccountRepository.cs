using RollblackLegacy.Auth.Domain;

namespace RollblackLegacy.Auth.Abstractions;

public interface ILegacyAccountRepository
{
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<LegacyAccountSchemaCapabilities> CreateAsync(
        LegacyAccountRegistration registration,
        CancellationToken cancellationToken);

    Task<AuthAccountRecord?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
}
