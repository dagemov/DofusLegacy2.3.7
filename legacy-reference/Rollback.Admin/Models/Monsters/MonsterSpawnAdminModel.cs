namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterSpawnAdminModel
{
    public int Id { get; set; }

    public int? MapId { get; set; }

    public short? SubAreaId { get; set; }

    public short MonsterId { get; set; }

    public string MonsterLabel { get; set; } = string.Empty;

    public sbyte MinGrade { get; set; } = 1;

    public sbyte MaxGrade { get; set; } = 1;

    public byte Probability { get; set; } = 5;

    public bool Disabled { get; set; }

    public string Source { get; set; } = "direct";
}
