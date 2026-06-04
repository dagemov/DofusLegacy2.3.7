namespace Rollback.Admin.Models.Items;

public sealed class ItemAuditSnapshot
{
    public ItemAuditStatus Status { get; set; } = ItemAuditStatus.Ambiguous;

    public string StatusLabel { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string IdentitySourceLabel { get; set; } = string.Empty;

    public bool IsRuntimeAvailable { get; set; }

    public bool HasReferenceIdentity { get; set; }

    public bool HasClientMetadata { get; set; }

    public bool HasDisplayName { get; set; }

    public bool HasClientIcon { get; set; }

    public bool HasManualAsset { get; set; }

    public bool IsLegacyRuntimeItem { get; set; }

    public IReadOnlyList<string> Differences { get; set; } = Array.Empty<string>();
}
