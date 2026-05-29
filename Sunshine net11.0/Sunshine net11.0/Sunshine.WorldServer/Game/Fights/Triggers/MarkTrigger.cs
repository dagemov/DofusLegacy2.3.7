using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sunshine.WorldServer.Game.Fights.Triggers
{
    public abstract class MarkTrigger
    {
        public event Action<MarkTrigger, FightActor, Spell> Triggered;

        protected void NotifyTriggered(FightActor target, Spell triggeredSpell)
        {
            var handler = Triggered;
            if (handler != null)
                handler(this, target, triggeredSpell);
        }

        public event Action<MarkTrigger> Removed;

        public void NotifyRemoved()
        {
            var handler = Removed;
            if (handler != null)
                handler(this);
        }

        public short Id { get; set; }
        public FightActor Caster { get; set; }
        public Spell CastedSpell { get; set; }
        public Effect OriginEffect { get; set; }
        public short CenterCell { get; set; }

        public Fight Fight => Caster.Fight;

        public MarkShape[] Shapes { get; set; }

        public abstract GameActionMarkTypeEnum Type { get; }
        public abstract TriggerTypeEnum TriggerType { get; }

        protected MarkTrigger(short id, FightActor caster, Spell castedSpell, Effect originEffect, short centerCell, params MarkShape[] shapes)
        {
            Id = id;
            Caster = caster;
            CastedSpell = castedSpell;
            OriginEffect = originEffect;
            CenterCell = centerCell;
            Shapes = shapes ?? new MarkShape[0];
        }

        public bool ContainsCell(short cell)
        {
            return Shapes != null && Shapes.Any(entry => entry != null && entry.GetCells().Contains(cell));
        }

        public IEnumerable<short> GetCells()
        {
            return Shapes == null ? Enumerable.Empty<short>() : Shapes.Where(entry => entry != null).SelectMany(entry => entry.GetCells()).Distinct();
        }

        public virtual bool TriggersOn(TriggerTypeEnum triggerType)
        {
            if (triggerType == TriggerTypeEnum.NEVER)
                return TriggerType == TriggerTypeEnum.NEVER;

            return (TriggerType & triggerType) == triggerType;
        }

        public virtual void Remove()
        {
            Fight.RemoveTrigger(this);
        }

        public virtual bool IsAffected(FightActor actor)
        {
            return true;
        }

        public abstract void Trigger(FightActor trigger, ObjectPosition firstPosition = null, int countPushed = 0);
        public abstract GameActionMark GetGameActionMark();
        public abstract GameActionMark GetHiddenGameActionMark();
        public abstract bool DoesSeeTrigger(FightActor fighter);
        public abstract bool DecrementDuration();
    }
}
