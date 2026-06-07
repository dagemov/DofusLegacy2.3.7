using Sunshine.Protocol.Enums;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_109)]
    public class SacrificeDamage : SpellEffectHandler
    {
        public override void Apply()
        {
            Effect.GenerateEffect();

            Damage damage = new Damage(EffectSchoolEnum.Neutral, (uint)Effect.Value, 0, Spell, Caster);
            Caster.InflictDamage(damage);
        }
    }
}
