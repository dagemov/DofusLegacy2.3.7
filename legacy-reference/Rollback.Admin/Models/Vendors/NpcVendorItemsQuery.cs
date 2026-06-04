using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Vendors;

public sealed class NpcVendorItemsQuery
{
    public string Search { get; set; } = string.Empty;

    public short? ItemId { get; set; }

    public short? MinLevel { get; set; }

    public short? MaxLevel { get; set; }

    public ItemType? TypeId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
