using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.Protocol.Utils;
using Sunshine.WorldServer.Game.Actors.Fighters;
using Sunshine.WorldServer.Game.Effects;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Spells;
using System;
using System.Drawing;
using System.Linq;
namespace Sunshine.WorldServer.Game.Fights.Triggers
{
	public class Trap : MarkTrigger
	{
		public Spell TrapSpell
		{
			get;
			set;
		}

		public GameActionFightInvisibilityStateEnum VisibleState
		{
			get;
			set;
		}

		public override GameActionMarkTypeEnum Type
		{
			get
			{
				return GameActionMarkTypeEnum.TRAP;
			}
		}

		public override TriggerTypeEnum TriggerType
		{
			get
			{
				return TriggerTypeEnum.MOVE;
			}
		}

		public Trap(short id, FightActor caster, Effect trapEffect, Spell castedSpell, Effect originEffect, Spell glyphSpell, short centerCell, byte size) 
            : base(id, caster, castedSpell, originEffect, centerCell, new MarkShape[] {
            new MarkShape(caster.Fight, centerCell, GameActionMarkCellsTypeEnum.CELLS_CIRCLE, size, Color.FromArgb(trapEffect.Value))
        })

        {         
			this.TrapSpell = glyphSpell;
			this.VisibleState = GameActionFightInvisibilityStateEnum.INVISIBLE;
            
		}

		public Trap(short id, FightActor caster, Effect trapEffect, Spell spell, Effect originEffect, Spell trapSpell, short centerCell, GameActionMarkCellsTypeEnum shape, byte size) 
            : base(id, caster, spell, originEffect, centerCell, new MarkShape[] {
            new MarkShape(caster.Fight, centerCell, shape, size, Color.FromArgb(trapEffect.Value))
		})
		{
			this.TrapSpell = trapSpell;
			this.VisibleState = GameActionFightInvisibilityStateEnum.INVISIBLE;
		}

		public override bool DoesSeeTrigger(FightActor fighter)
		{
			return this.VisibleState != GameActionFightInvisibilityStateEnum.INVISIBLE || fighter.IsFriendlyWith(Caster);
		}

		public override bool DecrementDuration()
		{
			return false;
		}

		public override void Trigger(FightActor fighter, ObjectPosition firstPosition = null, int countPushed = 0)
		{
			MarkShape[] shapes = base.Shapes;
			for (int i = 0; i < shapes.Length; i++)
                EffectDispatcher.Dispatch(Caster, TrapSpell, OriginEffect, fighter.Position.Cell, shapes[i].Cell, firstPosition, countPushed);
		}

		public override GameActionMark GetHiddenGameActionMark()
		{
			return new GameActionMark(base.Caster.Id, base.CastedSpell.Id, base.Id, (sbyte)this.Type, new GameActionMarkedCell[0]);
		}

		public override GameActionMark GetGameActionMark()
		{
			return new GameActionMark(base.Caster.Id, base.CastedSpell.Id, base.Id, (sbyte)this.Type, 
				from entry in base.Shapes
				select entry.GetGameActionMarkedCell());
		}        
	}
}
