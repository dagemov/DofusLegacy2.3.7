using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;
using System.Linq;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_RevealsInvisible)]
    public class RevealsInvisible : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (FightActor actor in GetAffectedActors())
            {
                if (actor == null)
                    continue;

                var invisBuffs = actor.GetBuffs(x => x is InvisibilityBuff).ToList();
                foreach (var buff in invisBuffs)
                    actor.RemoveBuff(buff);
            }
        }
    }
}
