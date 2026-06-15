using Sunshine.Protocol.Enums;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_DamageFix)]
    public class FixedDamage : SpellEffectHandler
    {
        public override void Apply()
        {
            var (diceNum, diceFace, fixedBonus) = EffectDamageResolver.ResolveDice(Effect);

            foreach (var actor in GetAffectedActors())
            {
                var damage = new Damage(EffectSchoolEnum.Neutral, diceNum, diceFace, Spell, Caster)
                {
                    FixedBonus = fixedBonus
                };
                actor.InflictDamage(damage);
            }
        }
    }
}
