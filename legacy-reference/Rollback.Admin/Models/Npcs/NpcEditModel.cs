namespace Rollback.Admin.Models.Npcs;

public sealed class NpcEditModel
{
    public short Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool Gender { get; set; }

    public string EntityLookString { get; set; } = string.Empty;

    public int? SpawnId { get; set; }

    public int MapId { get; set; }

    public short CellId { get; set; }

    public byte Direction { get; set; } = 1;

    public int? PrimaryActionId { get; set; }

    public NpcPrimaryActionMode PrimaryActionMode { get; set; } = NpcPrimaryActionMode.Shop;

    public string PrimaryActionAlias { get; set; } = "Shop";

    public string PrimaryActionParameters { get; set; } = "false";

    public string StringCriterion { get; set; } = string.Empty;

    public short Priority { get; set; }

    public bool ShopCanSell { get; set; }

    public short? TalkMessageId { get; set; }
}
