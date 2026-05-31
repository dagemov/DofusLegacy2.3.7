namespace RollblackLegacy.Auth.Contracts;

public sealed class AuthOperationResult
{
    public bool Success { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public int? AccountId { get; init; }

    public string? Username { get; init; }

    public string? Nickname { get; init; }

    public string? Email { get; init; }

    public bool EmailWasStored { get; init; }

    public bool UsesWebsiteContactTable { get; init; }
}
