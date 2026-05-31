
using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Fights.Buffs.Spells;

namespace Sunshine.WorldServer.Game.Effects.Spells.Debuffs
{
    [EffectHandler(EffectsEnum.Effect_1038)]
    public class Shield : SpellEffectHandler
    {     
        public override void Apply()
        {
            foreach (var current in GetAffectedActors())
                current.AddBuff(new StatsBuff(Caster, current, Spell, Effect, (short)Duration, true, StatsEnum.Shield, (short)Value, 1040));
        }
    }
}