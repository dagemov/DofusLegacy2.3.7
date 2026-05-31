namespace RollblackLegacy.Auth.Contracts;

public sealed class AuthLoginRequest
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
