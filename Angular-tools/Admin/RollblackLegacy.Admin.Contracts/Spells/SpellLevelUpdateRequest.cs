namespace RollblackLegacy.Admin.Contracts.Spells;

public sealed record SpellLevelUpdateRequest(
    int? ApCost,
    int? MinRange,
    int? MaxRange,
    bool? CastInLine,
    bool? CastInDiagonal,
    bool? CastTestLos,
    int? CriticalHitProbability,
    int? CriticalFailureProbability,
    bool? NeedFreeCell,
    bool? NeedTakenCell,
    int? MinCastInterval,
    int? InitialCooldown,
    int? MaxCastPerTurn,
    int? MaxCastPerTarget);
