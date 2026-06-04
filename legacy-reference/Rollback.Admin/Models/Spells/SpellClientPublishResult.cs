namespace Rollback.Admin.Models.Spells;

public sealed class SpellClientPublishResult
{
    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
