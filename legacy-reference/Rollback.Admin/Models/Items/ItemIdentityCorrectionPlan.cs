using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Items;

public sealed class ItemIdentityCorrectionPlan
{
    public short ItemId { get; set; }

    public bool CanApply { get; set; }

    public string Summary { get; set; } = string.Empty;

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public IReadOnlyList<ItemIdentityCorrectionChange> Changes { get; init; } = Array.Empty<ItemIdentityCorrectionChange>();

    public ItemType? CorrectedTypeId { get; set; }

    public short? CorrectedItemSetId { get; set; }

    public short? CorrectedAppearanceId { get; set; }

    public string? SuggestedOverrideName { get; set; }

    public string? SuggestedOverrideDescription { get; set; }
}
