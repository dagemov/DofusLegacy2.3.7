using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Items;

public sealed class AdminClientItemText
{
    public short ItemId { get; set; }

    public ItemType? ClientTypeId { get; set; }

    public int? NameId { get; set; }

    public int? DescriptionId { get; set; }

    public int? IconId { get; set; }

    public short? ClientAppearanceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
