using RollblackLegacy.Auth.Contracts;

namespace RollblackLegacy.Auth.Abstractions;

public interface IAuthService
{
    Task<AuthOperationResult> RegisterAsync(
        AuthRegisterRequest request,
        string? remoteIp,
        CancellationToken cancellationToken);

    Task<AuthOperationResult> LoginAsync(
        AuthLoginRequest request,
        CancellationToken cancellationToken);

    Task<UsernameAvailabilityResult> CheckUsernameAvailabilityAsync(
        string? username,
        CancellationToken cancellationToken);
}
