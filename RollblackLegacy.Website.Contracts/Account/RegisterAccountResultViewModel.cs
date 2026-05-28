namespace RollblackLegacy.Website.Contracts.Account;

public sealed class RegisterAccountResultViewModel
{
    public bool Succeeded { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public string? Username { get; init; }

    public string? Email { get; init; }

    public bool EmailWasStored { get; init; }

    public bool UsesWebsiteContactTable { get; init; }
}
