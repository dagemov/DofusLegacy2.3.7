namespace RollblackLegacy.Website.Contracts.Home;

public sealed class PlayStepsViewModel
{
    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required IReadOnlyList<PlayStepViewModel> Steps { get; init; }
}
