namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterGroupEditModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public List<MonsterGroupEntryAdminModel> Entries { get; set; } = new();

    public List<MonsterGroupAssignmentAdminModel> Assignments { get; set; } = new();
}
