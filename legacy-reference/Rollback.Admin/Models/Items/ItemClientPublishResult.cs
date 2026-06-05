namespace Rollback.Admin.Models.Items;

public sealed class ItemClientPublishResult
{
    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public bool HasWarnings =>
        Warnings.Count > 0;
}
