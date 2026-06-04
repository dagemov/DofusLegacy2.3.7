namespace Rollback.Admin.Models.Items;

public sealed class ReferenceItemIdentity
{
    public short ItemId { get; set; }

    public int NameId { get; set; }

    public int DescriptionId { get; set; }

    public short TypeId { get; set; }

    public string TypeLabel { get; set; } = string.Empty;

    public int IconId { get; set; }

    public short Level { get; set; }

    public short ItemSetId { get; set; }

    public int AppearanceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool HasResolvedText =>
        !string.IsNullOrWhiteSpace(Name) || !string.IsNullOrWhiteSpace(Description);
}
