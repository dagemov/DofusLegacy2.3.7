using Rollback.Admin.Models.GameEffects;

namespace Rollback.Admin.Models.Spells;

public sealed class SpellLevelEditModel
{
    public int LevelNumber { get; set; }

    public int Id { get; set; }

    public byte APCost { get; set; }

    public sbyte MinRange { get; set; }

    public sbyte MaxRange { get; set; }

    public bool CastInLine { get; set; }

    public bool CastTestLOS { get; set; }

    public bool NeedFreeCell { get; set; }

    public bool RangeCanBeBoosted { get; set; }

    public bool CriticalFailureEndsTurn { get; set; }

    public sbyte CriticalHitProbability { get; set; }

    public sbyte CriticalFailureProbability { get; set; }

    public byte MaxCastPerTurn { get; set; }

    public byte MaxCastPerTarget { get; set; }

    public byte MinCastInterval { get; set; }

    public byte MinPlayerLevel { get; set; }

    public List<GameEffectEditRow> Effects { get; set; } = new();

    public List<GameEffectEditRow> CriticalEffects { get; set; } = new();

    public List<short> StatesRequired { get; set; } = new();

    public List<short> StatesForbidden { get; set; } = new();
}
