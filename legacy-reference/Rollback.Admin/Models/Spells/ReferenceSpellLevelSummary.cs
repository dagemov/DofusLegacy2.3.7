namespace Rollback.Admin.Models.Spells;

public sealed class ReferenceSpellLevelSummary
{
    public int Id { get; init; }

    public short SpellId { get; init; }

    public int SpellBreed { get; init; }

    public int APCost { get; init; }

    public int MinRange { get; init; }

    public int MaxRange { get; init; }

    public bool CastInLine { get; init; }

    public bool CastInDiagonal { get; init; }

    public bool CastTestLos { get; init; }

    public int CriticalHitProbability { get; init; }

    public string StatesRequiredCsv { get; init; } = string.Empty;

    public int CriticalFailureProbability { get; init; }

    public bool NeedFreeCell { get; init; }

    public bool NeedFreeTrapCell { get; init; }

    public bool NeedTakenCell { get; init; }

    public bool RangeCanBeBoosted { get; init; }

    public int MaxStack { get; init; }

    public int MaxCastPerTurn { get; init; }

    public int MaxCastPerTarget { get; init; }

    public int MinCastInterval { get; init; }

    public int InitialCooldown { get; init; }

    public int GlobalCooldown { get; init; }

    public int MinPlayerLevel { get; init; }

    public bool CriticalFailureEndsTurn { get; init; }

    public bool HideEffects { get; init; }

    public bool Hidden { get; init; }

    public string StatesForbiddenCsv { get; init; } = string.Empty;

    public bool HasEffects { get; init; }

    public bool HasCriticalEffects { get; init; }
}
