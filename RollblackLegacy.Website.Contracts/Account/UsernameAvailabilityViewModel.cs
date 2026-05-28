namespace RollblackLegacy.Website.Contracts.Account;

public sealed class UsernameAvailabilityViewModel
{
    public bool HasValue { get; init; }

    public bool IsAvailable { get; init; }

    public required string Message { get; init; }

    public required string Tone { get; init; }
}
