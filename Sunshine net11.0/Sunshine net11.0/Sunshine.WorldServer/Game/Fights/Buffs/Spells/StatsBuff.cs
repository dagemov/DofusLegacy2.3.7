using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Actors.Look;
using Sunshine.WorldServer.Game.Actors.Stats;
using Sunshine.WorldServer.Game.Spells;
using Sunshine.WorldServer.Handlers.Actions;
using Sunshine.WorldServer.Handlers.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Fights.Buffs.Spells
{
    public class StatsBuff : Buff
    {
        public StatsEnum Stat { get; set; }

        public StatsEnum? Stat2 { get; set; }

        public short Value { get; set; }

        public bool IsMalus { get; set; }

        public StatsBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, StatsEnum stat, short value, short actionId, bool isMalus = false)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Stat = stat;
            Value = value;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
            ActionId = actionId;
            IsMalus = isMalus;
        }

        public StatsBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, StatsEnum stat, short value, bool isMalus = false)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Stat = stat;
            Value = value;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
            IsMalus = isMalus;
        }

        public StatsBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, StatsEnum stat, StatsEnum stat2, short value, bool isMalus = false) // Feca armor 
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            Stat = stat;
            Stat2 = stat2;
            Value = value;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
            IsMalus = isMalus;
        }


        public override void Apply()
        {
            bool lifeMaximumChanged = AffectsLifeMaximum();
            int lifeBefore = GetCurrentLife();

            Target.Stats[Stat].Context += Value;
            if (Stat2.HasValue)
                Target.Stats[Stat2.Value].Context += Value;

            if (lifeMaximumChanged)
            {
                PreserveCurrentLifeAfterMaxLifeChange(lifeBefore);
                Target.TryKillIfNoHealth(Caster);
            }
        }

        public override void Dispell()
        {
            bool lifeMaximumChanged = AffectsLifeMaximum();
            int lifeBefore = GetCurrentLife();

            Target.Stats[Stat].Context -= Value;
            if (Stat2.HasValue)
                Target.Stats[Stat2.Value].Context -= Value;

            if (lifeMaximumChanged)
            {
                PreserveCurrentLifeAfterMaxLifeChange(lifeBefore);
                Target.TryKillIfNoHealth(Caster);
            }
        }

        private bool AffectsLifeMaximum()
        {
            return Stat == StatsEnum.Health
                || Stat == StatsEnum.Vitality
                || (Stat2.HasValue && (Stat2.Value == StatsEnum.Health || Stat2.Value == StatsEnum.Vitality));
        }

        private int GetCurrentLife()
        {
            if (Target == null || Target.Stats == null || Target.Stats.Health == null)
                return 0;

            return Math.Max(0, Target.Stats.Health.Total);
        }

        private void PreserveCurrentLifeAfterMaxLifeChange(int lifeBefore)
        {
            if (Target == null || Target.Stats == null || Target.Stats.Health == null)
                return;

            int maxLife = Math.Max(1, Target.Stats.Health.TotalMax);
            int wantedLife = Math.Min(Math.Max(0, lifeBefore), maxLife);

            // Si la cible était vivante avant un boost/debuff de PV max, elle ne doit pas tomber à 0 PV
            // uniquement parce que son TotalMax a changé. La mort doit passer par InflictDamage/OnDead.
            if (lifeBefore > 0 && wantedLife <= 0)
                wantedLife = 1;

            int lifeAfter = wantedLife;
            Target.Stats.Health.Taken = Math.Max(0, maxLife - lifeAfter);
            Target.NormalizeFightHealth(false);

            int delta = lifeAfter - lifeBefore;

            // Un buff/débuff de PV max ne doit pas être affiché comme un soin.
            // On envoie seulement une perte si les PV courants dépassaient le nouveau maximum.
            if (Target.Fight != null && delta < 0)
                Target.Fight.OnLifePointsChanged(delta, Caster, Target);
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporaryBoostEffect(Id, Target.Id, Duration > 500 ? (short)-1 : Duration, Convert.ToSByte(Dispellable ? 0 : 1), 
                                                (short)Spell.Id, IsMalus ? (short)(Value * -1) : Value);
        }
    }
}
