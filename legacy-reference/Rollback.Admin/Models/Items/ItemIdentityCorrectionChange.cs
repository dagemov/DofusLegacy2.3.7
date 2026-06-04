namespace Rollback.Admin.Models.Items;

public sealed class ItemIdentityCorrectionChange
{
    public string Field { get; set; } = string.Empty;

    public string CurrentValue { get; set; } = string.Empty;

    public string SuggestedValue { get; set; } = string.Empty;

    public string SourceLabel { get; set; } = string.Empty;
}
