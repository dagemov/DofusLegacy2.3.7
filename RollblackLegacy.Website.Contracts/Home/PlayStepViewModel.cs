namespace RollblackLegacy.Website.Contracts.Home;

public sealed class PlayStepViewModel
{
    public int Number { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public string? Icon { get; init; }
}
