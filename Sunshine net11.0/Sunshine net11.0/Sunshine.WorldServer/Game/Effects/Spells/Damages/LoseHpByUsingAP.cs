using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_LoseHPByUsingAP)]
    public class LoseHpByUsingAP : SpellEffectHandler
    {
        public override void Apply()
        {
            Effect.GenerateEffect();
            int damagePerAp = Effect.Value;

            foreach (FightActor actor in GetAffectedActors())
            {
                if (actor == null || actor.IsDead() || damagePerAp <= 0)
                    continue;

                if (Duration > 0)
                    actor.AddBuff(new LoseHpByUsingApBuff(Caster, actor, Spell, Effect, (short)Duration, damagePerAp));
                else
                    actor.InflictDamage(damagePerAp, Caster, true);
            }
        }
    }
}
