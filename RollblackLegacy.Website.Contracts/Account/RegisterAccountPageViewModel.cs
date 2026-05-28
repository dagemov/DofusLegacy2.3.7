using RollblackLegacy.Website.Contracts.Branding;

namespace RollblackLegacy.Website.Contracts.Account;

public sealed class RegisterAccountPageViewModel
{
    public required BrandIdentityViewModel Brand { get; init; }

    public required RegisterAccountInputModel Form { get; init; }

    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required string SecurityHint { get; init; }

    public required string DiscordUrl { get; init; }

    public RegisterAccountResultViewModel? Result { get; init; }
}
