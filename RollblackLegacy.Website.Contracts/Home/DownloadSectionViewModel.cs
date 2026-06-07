using RollblackLegacy.Website.Contracts.Components;

namespace RollblackLegacy.Website.Contracts.Home;

public sealed class DownloadSectionViewModel
{
    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required ButtonAtomViewModel LauncherDownload { get; init; }

    public required string AdobeAirUrl { get; init; }

    public required string AdobeAirTitle { get; init; }

    public required string AdobeAirMessage { get; init; }

    public required ButtonAtomViewModel AdobeAirDownload { get; init; }
}
