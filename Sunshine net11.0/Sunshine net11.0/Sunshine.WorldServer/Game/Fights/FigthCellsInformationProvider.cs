using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using System;
namespace Sunshine.WorldServer.Game.Fights
{
	public class FigthCellsInformationProvider : ICellsInformationProvider
	{
		public Fight Fight
		{
			get;
			private set;
		}
		public Map Map
		{
			get
			{
				return this.Fight.Map;
			}
		}
		public FigthCellsInformationProvider(Fight fight)
		{
			this.Fight = fight;
		}
		public bool IsCellWalkable(short cell)
		{
			return this.Fight.IsCellFree(cell);
		}
		public virtual CellInformation GetCellInformation(short cell)
		{
			return new CellInformation(cell, this.IsCellWalkable(cell), true);
		}
	}
}
