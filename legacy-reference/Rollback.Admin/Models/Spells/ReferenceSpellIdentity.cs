namespace Rollback.Admin.Models.Spells;

public sealed class ReferenceSpellIdentity
{
    public short SpellId { get; init; }

    public int NameId { get; init; }

    public int DescriptionId { get; init; }

    public sbyte TypeId { get; init; }

    public string TypeLabel { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ScriptParams { get; init; } = string.Empty;

    public int ScriptId { get; init; }

    public int IconId { get; init; }

    public string SpellLevelsIdsCsv { get; init; } = string.Empty;

    public IReadOnlyList<int> OrderedLevelIds { get; init; } = Array.Empty<int>();

    public IReadOnlyDictionary<int, ReferenceSpellLevelSummary> LevelsById { get; init; } =
        new Dictionary<int, ReferenceSpellLevelSummary>();

    public IReadOnlyCollection<int> SpellBreeds { get; init; } = Array.Empty<int>();

    public bool HasClassicBreedLevels => SpellBreeds.Any(breed => breed is >= 1 and <= 12);

    public bool HasModernBreedLevels => SpellBreeds.Any(breed => breed > 12);

    public bool HasSupportLevels => SpellBreeds.Any(breed => breed == 0);
}
