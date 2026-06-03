namespace Rollback.Admin.Models.Items;

public sealed class ItemDiagnosticReport
{
    public short ItemId { get; set; }

    public RuntimeItemIdentitySnapshot? Runtime { get; set; }

    public string RuntimeSetName { get; set; } = string.Empty;

    public ReferenceItemIdentity? Reference { get; set; }

    public ReferenceItemSetIdentity? ReferenceSet { get; set; }

    public AdminClientItemText Client { get; set; } = new();

    public string OverrideName { get; set; } = string.Empty;

    public string OverrideDescription { get; set; } = string.Empty;

    public string ManualAssetRelativePath { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string DisplayDescription { get; set; } = string.Empty;

    public string NameSourceLabel { get; set; } = string.Empty;

    public string ClientBitmapFileName { get; set; } = string.Empty;

    public ItemAuditSnapshot Audit { get; set; } = new();

    public ItemClientVisibilitySnapshot ClientVisibility { get; set; } = new();
}
