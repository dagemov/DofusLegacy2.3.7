using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights.Buffs.Customs;

namespace Sunshine.WorldServer.Game.Effects.Spells.Damages
{
    [EffectHandler(EffectsEnum.Effect_StealHPAir), EffectHandler(EffectsEnum.Effect_StealHPNeutral),
     EffectHandler(EffectsEnum.Effect_StealHPWater), EffectHandler(EffectsEnum.Effect_StealHPFire),
     EffectHandler(EffectsEnum.Effect_StealHPEarth)]
    public class HpSteal : SpellEffectHandler
    {
        public override void Apply()
        {
            var effectSchool = GetEffectSchool(Id);

            foreach (var actor in GetAffectedActors())
            {
                if (Duration != 0)
                {
                    int buffId = actor.PopNextBuffId();
                    var triggerBuff = new TriggerBuff(
                        buffId,
                        actor,
                        Caster,
                        Effect,
                        Spell,
                        BuffTriggerType.TURN_BEGIN,
                        OnTurnBegin)
                    {
                        Duration = (short)Duration
                    };
                    actor.AddBuff(triggerBuff);
                    continue;
                }

                ApplyInstantSteal(actor, effectSchool);
            }
        }

        private static void OnTurnBegin(TriggerBuff buff, BuffTriggerType trigger, object token)
        {
            if (buff?.Target == null || buff.Caster == null || !buff.Target.IsAlive || buff.Effect == null)
                return;

            var school = GetEffectSchoolStatic(buff.Effect.Id);
            var damage = new Damage(school, buff.Effect.DiceNum, buff.Effect.DiceFace, buff.Spell, buff.Caster);
            buff.Target.InflictDamage(damage);
            buff.Caster.Heal(damage.Amount / 2, buff.Target);
        }

        private void ApplyInstantSteal(FightActor actor, EffectSchoolEnum effectSchool)
        {
            var damage = new Damage(effectSchool, DiceNum, DiceFace, Spell, Caster);
            actor.InflictDamage(damage);
            Caster.Heal(damage.Amount / 2, actor);
        }

        private EffectSchoolEnum GetEffectSchool(EffectsEnum effect) => GetEffectSchoolStatic(effect);

        private static EffectSchoolEnum GetEffectSchoolStatic(EffectsEnum effect)
        {
            switch (effect)
            {
                case EffectsEnum.Effect_StealHPWater:
                    return EffectSchoolEnum.Water;
                case EffectsEnum.Effect_StealHPEarth:
                    return EffectSchoolEnum.Earth;
                case EffectsEnum.Effect_StealHPAir:
                    return EffectSchoolEnum.Air;
                case EffectsEnum.Effect_StealHPFire:
                    return EffectSchoolEnum.Fire;
                case EffectsEnum.Effect_StealHPNeutral:
                    return EffectSchoolEnum.Neutral;
                default:
                    throw new System.Exception($"Effect {effect} has not associated School Type");
            }
        }
    }
}
