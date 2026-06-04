namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterGroupListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public int EntryCount { get; set; }

    public int AssignmentCount { get; set; }

    public DateTime? LastSyncedAt { get; set; }
}
