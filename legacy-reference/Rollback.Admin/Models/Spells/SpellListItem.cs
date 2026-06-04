namespace Rollback.Admin.Models.Spells;

public sealed class SpellListItem
{
    public short Id { get; set; }

    public sbyte TypeId { get; set; }

    public string TypeLabel { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int LevelCount { get; set; }

    public int CriticalLevelCount { get; set; }

    public byte MaxPlayerLevel { get; set; }

    public int? DisplayIconId { get; set; }

    public SpellAuditSnapshot Audit { get; set; } = new();
}
