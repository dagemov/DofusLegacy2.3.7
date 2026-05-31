namespace RollblackLegacy.Website.Contracts.Components;

public sealed class ButtonAtomViewModel
{
    public required string Label { get; init; }

    public required string Href { get; init; }

    public required string Variant { get; init; }

    public string? Icon { get; init; }

    public bool IsExternal { get; init; }

    public bool IsSubmit { get; init; }

    public string? Size { get; init; }

    public bool Glow { get; init; } = true;
}
