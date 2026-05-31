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
    public class SpellBuff : Buff
    {
        public Spell BoostedSpell
        {
            get;
            private set;
        }
        public short Value
        {
            get;
            private set;
        }

        public SpellBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, Spell boostedSpell, short value, short actionId)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            BoostedSpell = boostedSpell;
            Value = value;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
            ActionId = actionId;
        }

        public SpellBuff(FightActor caster, FightActor target, Spell spell, Effect effect,
            short duration, bool dispellable, Spell boostedSpell, short value)
        {
            Id = caster.PopNextBuffId();
            Caster = caster;
            Target = target;
            BoostedSpell = boostedSpell;
            Value = value;
            Spell = spell;
            Effect = effect;
            Duration = duration;
            Dispellable = dispellable;
        }

        public override void Apply()
        {
            Target.AddSpellBoost(BoostedSpell, Value);
        }

        public override void Dispell()
        {
            Target.RemoveSpellBoost(BoostedSpell, Value);
        }

        public override AbstractFightDispellableEffect GetAbstractFightDispellableEffect()
        {
            return new FightTemporarySpellBoostEffect(Id, Target.Id, Duration > 500 ? (short)-1 : (short)(Duration - 1), Convert.ToSByte(Dispellable ? 0 : 1), (short)Spell.Id, Value, (short)BoostedSpell.Id);

        }
    }
}
