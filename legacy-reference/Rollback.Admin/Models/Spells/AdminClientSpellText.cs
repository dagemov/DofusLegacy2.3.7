namespace Rollback.Admin.Models.Spells;

public sealed class AdminClientSpellText
{
    public short SpellId { get; set; }

    public short? TypeId { get; set; }

    public string TypeLabel { get; set; } = string.Empty;

    public int? NameId { get; set; }

    public int? DescriptionId { get; set; }

    public int? IconId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
