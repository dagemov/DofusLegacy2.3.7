namespace RollblackLegacy.Website.Domain.Accounts;

public sealed class LegacyAccountRegistration
{
    private LegacyAccountRegistration(
        string username,
        string email,
        string passwordHash,
        string registeredIp)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        RegisteredIp = registeredIp;
        Nickname = username;
        SecretQuestion = "registration-email";
        SecretAnswer = email;
    }

    public string Username { get; }

    public string Email { get; }

    public string PasswordHash { get; }

    public string Nickname { get; }

    public sbyte Role => 1;

    public string SecretQuestion { get; }

    public string SecretAnswer { get; }

    public bool IsBanned => false;

    public string Ticket => string.Empty;

    public string RegisteredIp { get; }

    public int Tokens => 0;

    public int NewTokens => 0;

    public static LegacyAccountRegistration Create(
        string username,
        string email,
        string passwordHash,
        string? registeredIp)
    {
        var normalizedUsername = (username ?? string.Empty).Trim();
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedIp = string.IsNullOrWhiteSpace(registeredIp)
            ? "website"
            : registeredIp.Trim();

        return new LegacyAccountRegistration(
            normalizedUsername,
            normalizedEmail,
            passwordHash,
            normalizedIp);
    }
}
