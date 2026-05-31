using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Types;
using Sunshine.WorldServer.Game.Fights;
using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Shapes;
using System;
using System.Drawing;
namespace Sunshine.WorldServer.Game.Fights.Triggers
{
    public class MarkShape
    {
        private readonly Zone m_zone;
        private readonly short[] m_cells;
        public Fight Fight
        {
            get;
            private set;
        }

        public short Cell
        {
            get;
            private set;
        }

        public GameActionMarkCellsTypeEnum Shape
        {
            get;
            private set;
        }

        public byte Size
        {
            get;
            private set;
        }

        public Color Color
        {
            get;
            private set;
        }

        public MarkShape(Fight fight, short cell, GameActionMarkCellsTypeEnum shape, byte size, Color color)
        {
            this.Fight = fight;
            this.Cell = cell;
            this.Shape = shape;
            this.Size = size;
            this.Color = color;
            this.m_zone = ((this.Shape == GameActionMarkCellsTypeEnum.CELLS_CROSS) ? new Zone(SpellShapeEnum.Q, size) : new Zone(SpellShapeEnum.C, size));
            this.m_cells = this.m_zone.GetCells(this.Cell, fight.Map);
        }

        public short[] GetCells()
        {
            return this.m_cells;
        }

        public GameActionMarkedCell GetGameActionMarkedCell()
        {
            return new GameActionMarkedCell(this.Cell, (sbyte)this.Size, this.Color.ToArgb() & 16777215, (sbyte)this.Shape);
        }
    }
}
