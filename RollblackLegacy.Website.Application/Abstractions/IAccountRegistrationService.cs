using RollblackLegacy.Website.Contracts.Account;

namespace RollblackLegacy.Website.Application.Abstractions;

public interface IAccountRegistrationService
{
    Task<RegisterAccountResultViewModel> RegisterAsync(
        RegisterAccountInputModel input,
        string? remoteIp,
        CancellationToken cancellationToken);

    Task<UsernameAvailabilityViewModel> CheckUsernameAvailabilityAsync(
        string? username,
        CancellationToken cancellationToken);
}
