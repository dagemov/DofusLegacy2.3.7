using Rollback.Protocol.Enums;

namespace Rollback.Admin.Models.Characters;

public sealed class AdminCharacterListItem
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public BreedEnum Breed { get; set; }

    public long Experience { get; set; }

    public int Kamas { get; set; }

    public int MapId { get; set; }

    public byte Level { get; set; }

    public GameHierarchyEnum AccountRole { get; set; }

    public IReadOnlyList<AdminCharacterSpecialSpellItem> AssignedSpecialSpells { get; set; } =
        Array.Empty<AdminCharacterSpecialSpellItem>();

    public bool CanReceiveAdminSpell =>
        AccountRole > GameHierarchyEnum.PLAYER;

    public bool CanGrantAdminSpell =>
        AccountRole > GameHierarchyEnum.PLAYER;

    public bool CanRevokeAdminSpell =>
        AssignedSpecialSpells.Count > 0;

    public bool HasSpecialSpell(short spellId) =>
        AssignedSpecialSpells.Any(x => x.SpellId == spellId);
}
