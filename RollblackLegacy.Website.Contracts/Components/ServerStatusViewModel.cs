namespace RollblackLegacy.Website.Contracts.Components;

public sealed class ServerStatusViewModel
{
    public required string Name { get; init; }

    public required string State { get; init; }

    public required string Summary { get; init; }

    public required bool IsOnline { get; init; }
}
