using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_AddErosion)]
    public class Erosion : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (FightActor actor in GetAffectedActors())
            {
                if (actor == null)
                    continue;

                Effect.GenerateEffect();

                if (Duration > 0)
                    actor.AddBuff(new StatsBuff(Caster, actor, Spell, Effect, (short)Duration, true, StatsEnum.Erosion, (short)Effect.Value));
                else
                    actor.AddStatBoost(StatsEnum.Erosion, (short)Effect.Value);
            }
        }
    }
}
