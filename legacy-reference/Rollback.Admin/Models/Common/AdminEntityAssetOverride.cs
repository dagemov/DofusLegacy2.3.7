namespace Rollback.Admin.Models.Common;

public sealed class AdminEntityAssetOverride
{
    public AdminEntityType EntityType { get; set; }

    public int EntityId { get; set; }

    public string AssetKind { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;
}
