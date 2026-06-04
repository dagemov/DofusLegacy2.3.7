using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Items;

public sealed class RuntimeItemIdentitySnapshot
{
    public short ItemId { get; set; }

    public ItemType TypeId { get; set; }

    public short Level { get; set; }

    public int Price { get; set; }

    public short ItemSetId { get; set; }

    public string RuntimeSetName { get; set; } = string.Empty;

    public short AppearanceId { get; set; }

    public bool HasEffects { get; set; }
}
