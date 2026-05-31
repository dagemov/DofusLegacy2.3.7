namespace RollblackLegacy.Website.Contracts.Components;

public sealed class LauncherCardViewModel
{
    public string? Eyebrow { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public string? Modifier { get; init; }
}
