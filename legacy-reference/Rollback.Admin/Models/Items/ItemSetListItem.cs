namespace Rollback.Admin.Models.Items;

public sealed class ItemSetListItem
{
    public short Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ReferenceName { get; set; } = string.Empty;

    public string NameSourceLabel { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public int BonusTierCount { get; set; }
}
