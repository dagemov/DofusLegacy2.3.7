using RollblackLegacy.Website.Contracts.Components;

namespace RollblackLegacy.Website.Contracts.Home;

public sealed class PlayJourneyViewModel
{
    public required string Eyebrow { get; init; }

    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required IReadOnlyList<ButtonAtomViewModel> HeaderActions { get; init; }

    public required IReadOnlyList<JourneyStepViewModel> Steps { get; init; }

    public required string AdobeAirTitle { get; init; }

    public required string AdobeAirMessage { get; init; }

    public required ButtonAtomViewModel AdobeAirDownload { get; init; }

    public required string FinalTitle { get; init; }

    public required string FinalSubtitle { get; init; }

    public required IReadOnlyList<ButtonAtomViewModel> FinalActions { get; init; }
}
