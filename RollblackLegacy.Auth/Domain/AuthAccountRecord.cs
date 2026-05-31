namespace RollblackLegacy.Auth.Domain;

public sealed class AuthAccountRecord
{
    public int Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public string Nickname { get; init; } = string.Empty;

    public bool IsBanned { get; init; }
}
