using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Vendors;

public sealed class NpcVendorItemEntry
{
    public int Id { get; set; }

    public int ShopActionId { get; set; }

    public short ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ItemType TypeId { get; set; }

    public string TypeLabel { get; set; } = string.Empty;

    public short Level { get; set; }

    public int Price { get; set; }
}
