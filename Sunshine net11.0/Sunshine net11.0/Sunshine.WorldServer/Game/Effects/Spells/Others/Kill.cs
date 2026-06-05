using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;

namespace Sunshine.WorldServer.Game.Effects.Spells.Others
{
    [EffectHandler(EffectsEnum.Effect_Kill)]
    public class Kill : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (var actor in GetAffectedActors())
            {
                if (actor != null && actor.IsAlive)
                    actor.Kill(Caster);
            }
        }
    }
}
