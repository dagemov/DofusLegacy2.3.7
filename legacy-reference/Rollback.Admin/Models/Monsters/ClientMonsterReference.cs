namespace Rollback.Admin.Models.Monsters;

public sealed class ClientMonsterReference
{
    public short MonsterId { get; set; }

    public int? NameId { get; set; }

    public int? GfxId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}
