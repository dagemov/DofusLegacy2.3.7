using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Items;

public sealed class AdminClientItemMetadata
{
    public short ItemId { get; set; }

    public ItemType? TypeId { get; set; }

    public int? NameId { get; set; }

    public int? DescriptionId { get; set; }

    public int? IconId { get; set; }

    public short? AppearanceId { get; set; }
}
