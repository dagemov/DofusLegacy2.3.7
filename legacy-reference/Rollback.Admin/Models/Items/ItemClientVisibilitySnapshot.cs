namespace Rollback.Admin.Models.Items;

public sealed class ItemClientVisibilitySnapshot
{
    public bool HasClientDefinition { get; set; }

    public bool HasResolvedText { get; set; }

    public bool HasBitmapMapping { get; set; }

    public bool HasBitmapAsset { get; set; }

    public bool UsesManualAdminAsset { get; set; }

    public bool IsClientVisibleEnough { get; set; }

    public string StatusLabel { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public IReadOnlyList<string> Details { get; init; } = Array.Empty<string>();
}
