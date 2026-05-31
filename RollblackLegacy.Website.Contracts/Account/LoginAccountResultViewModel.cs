namespace RollblackLegacy.Website.Contracts.Account;

public sealed class LoginAccountResultViewModel
{
    public bool Succeeded { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int? AccountId { get; init; }

    public string? Username { get; init; }

    public string? Nickname { get; init; }
}
