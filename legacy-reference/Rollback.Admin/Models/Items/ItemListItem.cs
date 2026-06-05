using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Items;

public sealed class ItemListItem
{
    public short Id { get; set; }

    public ItemType TypeId { get; set; }

    public string TypeLabel { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string NameSourceLabel { get; set; } = string.Empty;

    public short Level { get; set; }

    public int Price { get; set; }

    public short ItemSetId { get; set; }

    public short AppearanceId { get; set; }

    public int? ClientIconId { get; set; }

    public short? ClientAppearanceId { get; set; }

    public string PreviewImageUrl { get; set; } = string.Empty;

    public string ManualPreviewImageUrl { get; set; } = string.Empty;

    public string PreviewFallbackLabel { get; set; } = string.Empty;
}
