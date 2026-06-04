namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterGroupAssignmentAdminModel
{
    public int Id { get; set; }

    public int MonsterGroupId { get; set; }

    public int? MapId { get; set; }

    public short? SubAreaId { get; set; }

    public byte? ProbabilityOverride { get; set; }

    public bool Disabled { get; set; }

    public string TargetLabel { get; set; } = string.Empty;

    public DateTime? LastSyncedAt { get; set; }
}
