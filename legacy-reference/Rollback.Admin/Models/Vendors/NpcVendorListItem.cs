namespace Rollback.Admin.Models.Vendors;

public sealed class NpcVendorListItem
{
    public int ShopActionId { get; set; }

    public short NpcId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int MapId { get; set; }

    public short CellId { get; set; }

    public int ItemCount { get; set; }

    public string CategoryLabel { get; set; } = string.Empty;

    public string DisplayName =>
        $"{Name} [{NpcId}]";
}
