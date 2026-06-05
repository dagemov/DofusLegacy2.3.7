using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Items;

public sealed class ItemCatalogQuery
{
    public string Search { get; set; } = string.Empty;

    public IReadOnlyCollection<ItemType> Types { get; set; } = Array.Empty<ItemType>();

    public IReadOnlyCollection<short> ExcludedItemIds { get; set; } = Array.Empty<short>();

    public short? MinLevel { get; set; }

    public short? MaxLevel { get; set; }

    public int MaxResults { get; set; }
}
