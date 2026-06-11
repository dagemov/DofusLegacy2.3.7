using Sunshine.Protocol.Enums;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_109)]
    public class SacrificeDamage : SpellEffectHandler
    {
        public override void Apply()
        {
            Effect.GenerateEffect();

            var actors = GetAffectedActors()?.Where(x => x != null && x.IsAlive).ToList();
            if (actors == null || actors.Count == 0)
                actors = new System.Collections.Generic.List<Game.Actors.Fighters.FightActor> { Caster };

            foreach (var actor in actors)
            {
                var damage = new Damage(EffectSchoolEnum.Neutral, (uint)Effect.Value, 0, Spell, Caster);
                actor.InflictDamage(damage);
            }
        }
    }
}
