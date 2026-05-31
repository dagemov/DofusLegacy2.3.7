using RollblackLegacy.Website.Contracts.Branding;

namespace RollblackLegacy.Website.Contracts.Account;

public sealed class LoginAccountPageViewModel
{
    public required BrandIdentityViewModel Brand { get; init; }

    public required LoginAccountInputModel Form { get; init; }

    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required string DiscordUrl { get; init; }

    public LoginAccountResultViewModel? Result { get; init; }
}
