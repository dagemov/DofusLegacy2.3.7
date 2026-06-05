namespace Rollback.Admin.Models.Spells;

public sealed class AdminClientSpellMetadata
{
    public short SpellId { get; set; }

    public short? TypeId { get; set; }

    public int? NameId { get; set; }

    public int? DescriptionId { get; set; }

    public int? IconId { get; set; }

    public string ScriptParams { get; set; } = string.Empty;

    public int? ScriptId { get; set; }
}
