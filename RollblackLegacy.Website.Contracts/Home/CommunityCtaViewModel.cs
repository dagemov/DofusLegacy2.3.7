using RollblackLegacy.Website.Contracts.Components;

namespace RollblackLegacy.Website.Contracts.Home;

public sealed class CommunityCtaViewModel
{
    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required ButtonAtomViewModel DiscordButton { get; init; }
}
