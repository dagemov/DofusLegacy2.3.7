using RollblackLegacy.Auth.Contracts;

namespace RollblackLegacy.Website.Application.Abstractions;

public interface IOneLauncherApiClient
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
