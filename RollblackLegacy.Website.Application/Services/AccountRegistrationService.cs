using RollblackLegacy.Auth.Contracts;
using RollblackLegacy.Website.Application.Abstractions;
using RollblackLegacy.Website.Contracts.Account;

namespace RollblackLegacy.Website.Application.Services;

public sealed class AccountRegistrationService : IAccountRegistrationService
{
    private readonly IOneLauncherApiClient _apiClient;

    public AccountRegistrationService(IOneLauncherApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<RegisterAccountResultViewModel> RegisterAsync(
        RegisterAccountInputModel input,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        AuthOperationResult result = await _apiClient.RegisterAsync(
            new AuthRegisterRequest
            {
                Username = input.Username,
                Email = input.Email,
                Password = input.Password,
                ConfirmPassword = input.ConfirmPassword,
            },
            remoteIp,
            cancellationToken);

        return new RegisterAccountResultViewModel
        {
            Succeeded = result.Success,
            Title = result.Title,
            Message = result.Message,
            Username = result.Username,
            Email = result.Email,
            EmailWasStored = result.EmailWasStored,
            UsesWebsiteContactTable = result.UsesWebsiteContactTable,
        };
    }

    public async Task<UsernameAvailabilityViewModel> CheckUsernameAvailabilityAsync(
        string? username,
        CancellationToken cancellationToken)
    {
        UsernameAvailabilityResult result = await _apiClient.CheckUsernameAvailabilityAsync(
            username,
            cancellationToken);

        return new UsernameAvailabilityViewModel
        {
            HasValue = result.HasValue,
            IsAvailable = result.IsAvailable,
            Message = result.Message,
            Tone = result.Tone,
        };
    }
}
