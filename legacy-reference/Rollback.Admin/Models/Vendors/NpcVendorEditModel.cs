using Rollback.Admin.Models.Common;

namespace Rollback.Admin.Models.Vendors;

public sealed class NpcVendorEditModel
{
    public int ShopActionId { get; set; }

    public short NpcId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int MapId { get; set; }

    public short CellId { get; set; }

    public string CategoryLabel { get; set; } = string.Empty;

    public AdminPagedResult<NpcVendorItemEntry> ItemPage { get; set; } =
        new(Array.Empty<NpcVendorItemEntry>(), 0, 1, 10);

    public IReadOnlyList<NpcVendorItemEntry> Items =>
        ItemPage.Items;

    public string DisplayName =>
        $"{Name} [{NpcId}]";
}
