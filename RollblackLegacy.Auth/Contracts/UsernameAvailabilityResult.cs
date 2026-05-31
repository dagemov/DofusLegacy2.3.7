namespace RollblackLegacy.Auth.Contracts;

public sealed class UsernameAvailabilityResult
{
    public bool HasValue { get; init; }

    public bool IsAvailable { get; init; }

    public string Message { get; init; } = string.Empty;

    public string Tone { get; init; } = "muted";
}
