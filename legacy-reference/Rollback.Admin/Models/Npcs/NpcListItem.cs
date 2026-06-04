namespace Rollback.Admin.Models.Npcs;

public sealed class NpcListItem
{
    public short Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool Gender { get; set; }

    public string GenderLabel =>
        Gender ? "F" : "M";

    public int SpawnCount { get; set; }

    public int? MapId { get; set; }

    public short? CellId { get; set; }

    public string ActionLabel { get; set; } = string.Empty;

    public string EntityLookString { get; set; } = string.Empty;
}
