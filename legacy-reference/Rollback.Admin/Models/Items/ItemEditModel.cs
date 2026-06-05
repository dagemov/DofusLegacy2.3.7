using Rollback.Admin.Models.GameEffects;
using Rollback.World.CustomEnums;

namespace Rollback.Admin.Models.Items;

public sealed class ItemEditModel
{
    public short Id { get; set; }

    public ItemType TypeId { get; set; } = ItemType.Amulette;

    public short Level { get; set; }

    public int Weight { get; set; }

    public bool Usable { get; set; }

    public bool Targetable { get; set; }

    public bool Etheral { get; set; }

    public int Price { get; set; }

    public short ItemSetId { get; set; }

    public string StringCriterion { get; set; } = string.Empty;

    public short AppearanceId { get; set; }

    public string RecipesCsv { get; set; } = string.Empty;

    public bool TwoHanded { get; set; }

    public short APCost { get; set; }

    public sbyte MinRange { get; set; }

    public sbyte MaxRange { get; set; }

    public bool CastInLine { get; set; }

    public bool CastTestLOS { get; set; }

    public sbyte CriticalHitProbability { get; set; }

    public sbyte CriticalHitBonus { get; set; }

    public sbyte CriticalFailureProbability { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string OverrideName { get; set; } = string.Empty;

    public string OverrideDescription { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string ClientDescription { get; set; } = string.Empty;

    public int? ClientIconId { get; set; }

    public short? ClientAppearanceId { get; set; }

    public string NameSourceLabel { get; set; } = string.Empty;

    public string ManualAssetRelativePath { get; set; } = string.Empty;

    public string ManualImageUrl { get; set; } = string.Empty;

    public int? ReferenceNameId { get; set; }

    public int? ReferenceDescriptionId { get; set; }

    public int? ReferenceIconId { get; set; }

    public short? ReferenceTypeId { get; set; }

    public string ReferenceTypeLabel { get; set; } = string.Empty;

    public short? ReferenceSetId { get; set; }

    public string ReferenceSetName { get; set; } = string.Empty;

    public ItemAuditSnapshot Audit { get; set; } = new();

    public ItemClientVisibilitySnapshot ClientVisibility { get; set; } = new();

    public ItemIdentityCorrectionPlan IdentityCorrectionPlan { get; set; } = new();

    public List<GameEffectEditRow> Effects { get; set; } = new();
}
