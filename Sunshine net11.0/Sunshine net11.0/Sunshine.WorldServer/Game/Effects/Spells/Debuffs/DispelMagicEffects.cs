using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_DispelMagicEffects)]
    public class DispelMagicEffects : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (FightActor actor in GetAffectedActors())
            {
                if (actor == null)
                    continue;

                var buffs = actor.GetBuffs(x => x != null && x.Dispellable).ToArray();
                foreach (var buff in buffs)
                    actor.RemoveBuff(buff);
            }
        }
    }
}
