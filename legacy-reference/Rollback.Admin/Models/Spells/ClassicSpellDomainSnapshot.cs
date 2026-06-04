namespace Rollback.Admin.Models.Spells;

public sealed class ClassicSpellDomainSnapshot
{
    public IReadOnlyCollection<short> RuntimeSpellIds { get; init; } = Array.Empty<short>();

    public IReadOnlyCollection<short> ClassicBreedSpellIds { get; init; } = Array.Empty<short>();

    public IReadOnlyCollection<short> ReferenceClassicSpellIds { get; init; } = Array.Empty<short>();

    public IReadOnlyCollection<short> ReferenceModernSpellIds { get; init; } = Array.Empty<short>();

    public IReadOnlyCollection<short> ReferenceSupportSpellIds { get; init; } = Array.Empty<short>();

    public IReadOnlyCollection<short> AdminSpellIds { get; init; } = Array.Empty<short>();

    public int ExcludedModernReferenceCount { get; init; }

    public SpellDomainClassification Classify(short spellId)
    {
        if (ClassicBreedSpellIds.Contains(spellId) || ReferenceClassicSpellIds.Contains(spellId))
            return SpellDomainClassification.ClassicClass;

        if (RuntimeSpellIds.Contains(spellId))
            return SpellDomainClassification.SupportOrCommon;

        if (ReferenceModernSpellIds.Contains(spellId))
            return SpellDomainClassification.ExcludedModernClass;

        return SpellDomainClassification.Ambiguous;
    }
}
