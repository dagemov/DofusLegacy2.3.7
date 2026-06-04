namespace Rollback.Admin.Models.Spells;

public sealed class SpellEditModel
{
    public short Id { get; set; }

    public sbyte TypeId { get; set; }

    public string TypeLabel { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string OverrideName { get; set; } = string.Empty;

    public string OverrideDescription { get; set; } = string.Empty;

    public string ReferenceName { get; set; } = string.Empty;

    public string ReferenceDescription { get; set; } = string.Empty;

    public int? ReferenceNameId { get; set; }

    public int? ReferenceDescriptionId { get; set; }

    public sbyte? ReferenceTypeId { get; set; }

    public string ReferenceTypeLabel { get; set; } = string.Empty;

    public int? ReferenceIconId { get; set; }

    public string ReferenceLevelIdsCsv { get; set; } = string.Empty;

    public string RuntimeLevelIdsCsv { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string ClientDescription { get; set; } = string.Empty;

    public int? ClientIconId { get; set; }

    public int? DisplayIconId { get; set; }

    public List<int> AssignedBreedIds { get; set; } = new();

    public List<int> ReferenceBreedIds { get; set; } = new();

    public bool RuntimeExists { get; set; } = true;

    public SpellAuditSnapshot Audit { get; set; } = new();

    public List<SpellLevelEditModel> Levels { get; set; } = new();
}
