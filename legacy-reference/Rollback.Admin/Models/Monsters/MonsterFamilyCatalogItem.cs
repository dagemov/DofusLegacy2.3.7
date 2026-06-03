namespace Rollback.Admin.Models.Monsters;

public sealed class MonsterFamilyCatalogItem
{
    public byte Race { get; set; }

    public string Label { get; set; } = string.Empty;

    public int MonsterCount { get; set; }

    public short MinLevel { get; set; }

    public short MaxLevel { get; set; }

    public string SampleMonsters { get; set; } = string.Empty;
}
