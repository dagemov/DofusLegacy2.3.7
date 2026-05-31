using RollblackLegacy.Auth.Contracts;
using RollblackLegacy.Website.Application.Abstractions;
using RollblackLegacy.Website.Contracts.Account;

namespace RollblackLegacy.Website.Application.Services;

public sealed class AccountLoginService : IAccountLoginService
{
    private readonly IOneLauncherApiClient _apiClient;

    public AccountLoginService(IOneLauncherApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<LoginAccountResultViewModel> LoginAsync(
        LoginAccountInputModel input,
        CancellationToken cancellationToken)
    {
        AuthOperationResult result = await _apiClient.LoginAsync(
            new AuthLoginRequest
            {
                Username = input.Username,
                Password = input.Password,
            },
            cancellationToken);

        return new LoginAccountResultViewModel
        {
            Succeeded = result.Success,
            Title = result.Title,
            Message = result.Message,
            AccountId = result.AccountId,
            Username = result.Username,
            Nickname = result.Nickname,
        };
    }
}
