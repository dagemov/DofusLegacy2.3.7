using RollblackLegacy.Website.Contracts.Components;

namespace RollblackLegacy.Website.Contracts.Home;

public sealed class JourneyStepViewModel
{
    public int StepNumber { get; init; }

    public required string StepLabel { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required IReadOnlyList<string> Bullets { get; init; }

    public required string ImagePath { get; init; }

    public required string ImageAlt { get; init; }

    public bool ReverseLayout { get; init; }

    public ButtonAtomViewModel? Cta { get; init; }
}
