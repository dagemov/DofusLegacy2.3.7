using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;

namespace Sunshine.WorldServer.Game.Effects.Spells.Buffs
{
    [EffectHandler(EffectsEnum.Effect_Sacrifice)]
    public class Sacrifice : SpellEffectHandler
    {
        public override void Apply()
        {
            foreach (FightActor current in GetAffectedActors())
            {
                if (current == Caster)
                    continue;

                current.AddBuff(new SacrificeBuff(Caster, current, Spell, Effect, (short)Duration, true));
            }
        }
    }
}