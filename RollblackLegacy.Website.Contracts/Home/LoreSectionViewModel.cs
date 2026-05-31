namespace RollblackLegacy.Website.Contracts.Home;

public sealed class LoreSectionViewModel
{
    public required string Eyebrow { get; init; }

    public required string Title { get; init; }

    public required IReadOnlyList<string> Paragraphs { get; init; }
}
