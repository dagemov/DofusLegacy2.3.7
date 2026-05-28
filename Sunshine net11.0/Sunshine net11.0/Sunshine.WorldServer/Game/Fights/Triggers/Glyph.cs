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
    public class Glyph : MarkTrigger
    {
        private static readonly int[] SPELLS_GLYPH_END_TURN = new int[]
        {
            13,
            2035
        };

        public Spell GlyphSpell
        {
            get;
            set;
        }

        public int Duration
        {
            get;
            private set;
        }

        public override GameActionMarkTypeEnum Type
        {
            get
            {
                return GameActionMarkTypeEnum.GLYPH;
            }
        }

        public override TriggerTypeEnum TriggerType
        {
            get
            {
                return Glyph.SPELLS_GLYPH_END_TURN.Contains(base.CastedSpell.Id) ? TriggerTypeEnum.TURN_END : TriggerTypeEnum.TURN_BEGIN;
            }
        }

        public Glyph(short id, FightActor caster, Effect glyphEffect, Spell castedSpell, Effect originEffect, Spell glyphSpell, short centerCell, byte size)
            : base(id, caster, castedSpell, originEffect, centerCell, new MarkShape[] {
            new MarkShape(caster.Fight, centerCell, GameActionMarkCellsTypeEnum.CELLS_CIRCLE, size, Color.FromArgb(glyphEffect.Value))
        })

        {
            this.GlyphSpell = glyphSpell;
            this.Duration = glyphEffect.Duration;
        }

        public Glyph(short id, FightActor caster, Effect glyphEffect, Spell spell, Effect originEffect, Spell glyphSpell, short centerCell, GameActionMarkCellsTypeEnum shape, byte size)
            : base(id, caster, spell, originEffect, centerCell, new MarkShape[] {
            new MarkShape(caster.Fight, centerCell, shape, size, Color.FromArgb(glyphEffect.Value))
        })
        {
            this.GlyphSpell = glyphSpell;
            this.Duration = glyphEffect.Duration;
        }

        public override bool DoesSeeTrigger(FightActor fighter)
        {
            return true;
        }

        public override bool DecrementDuration()
        {
            return this.Duration-- <= 0;
        }

        public override void Trigger(FightActor fighter, ObjectPosition firstPosition = null, int countPushed = 0)
        {
            MarkShape[] shapes = base.Shapes;
            for (int i = 0; i < shapes.Length; i++)
                EffectDispatcher.Dispatch(Caster, GlyphSpell, OriginEffect, fighter.Position.Cell, shapes[i].Cell, firstPosition, countPushed);
        }

        public override GameActionMark GetHiddenGameActionMark()
        {
            return new GameActionMark();
        }

        public override GameActionMark GetGameActionMark()
        {
            return new GameActionMark(base.Caster.Id, base.CastedSpell.Id, base.Id, (sbyte)this.Type,
                from entry in base.Shapes
                select entry.GetGameActionMarkedCell());
        }
    }
}
