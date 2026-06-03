namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterListItem
{
    public short Id { get; set; }

    public byte Race { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string NameSource { get; set; } = string.Empty;

    public string ClientDisplayName { get; set; } = string.Empty;

    public int? ClientNameId { get; set; }

    public int? ClientGfxId { get; set; }

    public bool HasNameMismatch { get; set; }

    public string EntityLookString { get; set; } = string.Empty;

    public short MinLevel { get; set; }

    public short MaxLevel { get; set; }

    public int GradeCount { get; set; }

    public int SpawnCount { get; set; }

    public int RareSpawnCount { get; set; }

    public int SpellCount { get; set; }

    public int DropCount { get; set; }

    public string ManualPreviewImageUrl { get; set; } = string.Empty;
}
