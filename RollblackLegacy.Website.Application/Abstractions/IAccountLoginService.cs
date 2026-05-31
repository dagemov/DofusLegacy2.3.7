using RollblackLegacy.Website.Contracts.Account;

namespace RollblackLegacy.Website.Application.Abstractions;

public interface IAccountLoginService
{
    Task<LoginAccountResultViewModel> LoginAsync(
        LoginAccountInputModel input,
        CancellationToken cancellationToken);
}
