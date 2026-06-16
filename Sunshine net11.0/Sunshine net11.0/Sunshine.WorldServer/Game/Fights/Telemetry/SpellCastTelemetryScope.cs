using System;
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Spells;

namespace Sunshine.WorldServer.Game.Fights.Telemetry
{
    /// <summary>
    /// Correlates spell/effect events for a single cast on the current thread.
    /// </summary>
    public sealed class SpellCastTelemetryScope : IDisposable
    {
        [ThreadStatic]
        private static SpellCastTelemetryScope _current;

        public static SpellCastTelemetryScope Current => _current;

        public string CorrelationId { get; }
        public Fight Fight { get; }
        public FightActor Caster { get; }
        public Spell Spell { get; }
        public short TargetCell { get; }
        public int ApBefore { get; }
        public FightSpellCastCriticalEnum Critical { get; private set; }
        public bool IsCriticalEffectSet { get; private set; }

        private SpellCastTelemetryScope(
            string correlationId,
            Fight fight,
            FightActor caster,
            Spell spell,
            short targetCell,
            int apBefore)
        {
            CorrelationId = correlationId;
            Fight = fight;
            Caster = caster;
            Spell = spell;
            TargetCell = targetCell;
            ApBefore = apBefore;
        }

        public static SpellCastTelemetryScope Begin(Fight fight, FightActor caster, Spell spell, short targetCell, int apBefore)
        {
            var previous = _current;
            var correlationId = SpellEffectTelemetry.AllocateCorrelationId(fight, caster);
            _current = new SpellCastTelemetryScope(correlationId, fight, caster, spell, targetCell, apBefore);
            return _current;
        }

        public void SetCritical(FightSpellCastCriticalEnum critical, bool isCriticalEffectSet)
        {
            Critical = critical;
            IsCriticalEffectSet = isCriticalEffectSet;
        }

        public void Dispose()
        {
            _current = null;
        }
    }
}
