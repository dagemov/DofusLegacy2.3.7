namespace RollblackLegacy.Website.Contracts.Components;

public sealed class BadgeViewModel
{
    public required string Label { get; init; }

    public required string Tone { get; init; }

    public string? IconPath { get; init; }
}
