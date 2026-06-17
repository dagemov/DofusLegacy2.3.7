using Dapper.Contrib.Extensions;

namespace Sunshine.MySql.Database.World.Spells
{
    /// <summary>
    /// Per-effect metadata override, keyed by (SpellId, EffectId). Mirrors the `effect_metadata`
    /// side table proposed in docs/metadata-schema-proposal.md (section 2).
    ///
    /// Phase 1 is shadow-only: these rows are read and logged for parity, but the legacy hardcode
    /// in the handlers still governs behavior. Absence of a row means "use current behavior".
    /// </summary>
    [Table("effect_metadata")]
    public class EffectMetadataRecord
    {
        [ExplicitKey]
        public int SpellId { get; set; }

        [ExplicitKey]
        public int EffectId { get; set; }

        // 0=affected (default), 1=caster, 2=summon, 3=target
        public int KillTarget { get; set; }

        // stateId required to grant the bonus (0 = none)
        public int RequiresState { get; set; }

        // 1 = apply BonusMultiplier when the caster has RequiresState
        public int BonusIfState { get; set; }

        // damage/value multiplier (2.00 = x2)
        public decimal BonusMultiplier { get; set; }

        // state applied to the caster after a successful cast (Colere: State_51)
        public int GrantsStateOnCast { get; set; }

        // 1 = this heal/effect is allowed to affect enemies
        public int AllowEnemyTarget { get; set; }

        // 0=turn_begin (default), 1=turn_end (glyphs)
        public int TriggerTiming { get; set; }
    }

    public enum KillTargetType
    {
        Affected = 0,
        Caster = 1,
        Summon = 2,
        Target = 3
    }

    public enum TriggerTimingType
    {
        TurnBegin = 0,
        TurnEnd = 1
    }
}
