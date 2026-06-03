namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterGroupSyncResult
{
    public int AssignmentId { get; set; }

    public int UpsertedCount { get; set; }

    public int InsertedCount { get; set; }

    public int UpdatedCount { get; set; }
}
