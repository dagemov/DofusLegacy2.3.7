using Sunshine.WorldServer.Game.Fights.Triggers;
using Sunshine.Protocol.Enums;
using System;
namespace Sunshine.WorldServer.Game.Maps.Pathfinding
{
	public class CellInformation
	{
		public short Cell
		{
			get;
			set;
		}
		public bool Walkable
		{
			get;
			set;
		}
		public bool Fighting
		{
			get;
			set;
		}
		public bool UseAI
		{
			get;
			set;
		}
		public int Efficience
		{
			get;
			set;
		}
		public Trap Trap
		{
			get;
			set;
		}
        public Glyph Glyph
        {
            get;
            set;
        }
        public CellInformation(short cell, bool walkable)
		{
			this.Cell = cell;
			this.Walkable = walkable;
		}
		public CellInformation(short cell, bool walkable, bool fighting)
		{
			this.Cell = cell;
			this.Walkable = walkable;
			this.Fighting = fighting;
		}
		public CellInformation(short cell, bool walkable, bool fighting, bool useAI, int efficience, Trap trap, Glyph glyph)
		{
			this.Cell = cell;
			this.Walkable = walkable;
			this.Fighting = fighting;
			this.UseAI = useAI;
			this.Efficience = efficience;
			this.Trap = trap;
			this.Glyph = glyph;
		}
	}
}
