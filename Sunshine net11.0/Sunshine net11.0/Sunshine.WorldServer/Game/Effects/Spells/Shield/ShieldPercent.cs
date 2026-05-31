using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_AddShieldPercent)]
    public class ShieldPercent : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var current in GetAffectedActors())
            {
                Effect.GenerateEffect();
                var shieldValue = Caster.Stats.Health.TotalMax*(Effect.Value/100.0);
                current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.Shield, (short)shieldValue, 1040));
            }
        }
    }
}