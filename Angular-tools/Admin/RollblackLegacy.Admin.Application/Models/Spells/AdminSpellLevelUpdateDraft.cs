namespace RollblackLegacy.Admin.Application.Models.Spells;

public sealed record AdminSpellLevelUpdateDraft(
    int ApCost,
    int MinRange,
    int MaxRange,
    bool CastInLine,
    bool CastTestLos,
    int CriticalHitProbability,
    int CriticalFailureProbability,
    bool NeedFreeCell,
    int MinCastInterval,
    int MaxCastPerTurn,
    int MaxCastPerTarget,
    bool? CastInDiagonal,
    bool? NeedTakenCell,
    int? InitialCooldown);
