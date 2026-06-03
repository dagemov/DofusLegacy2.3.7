namespace Rollback.Admin.Models.Spells;

public sealed class SpellReferenceSummary
{
    public int CharacterCount { get; set; }

    public int BreedCount { get; set; }

    public int MonsterCount { get; set; }

    public int NpcLearnReplyCount { get; set; }

    public sbyte MaxCharacterLevel { get; set; }

    public sbyte MaxMonsterLevel { get; set; }

    public bool HasBlockingReferences =>
        CharacterCount > 0 ||
        BreedCount > 0 ||
        MonsterCount > 0 ||
        NpcLearnReplyCount > 0;

    public sbyte MaxReferencedLevel =>
        (sbyte)Math.Max(MaxCharacterLevel, MaxMonsterLevel);
}
