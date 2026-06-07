namespace RollblackLegacy.Website.Contracts.Home;

public sealed class InvitationBulletViewModel
{
    public required string Icon { get; init; }

    public string? IconEmoji { get; init; }

    public required string Title { get; init; }

    public required string Text { get; init; }
}
