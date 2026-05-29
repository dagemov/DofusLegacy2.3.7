using Sunshine.WorldServer.Game.Maps;
using Sunshine.WorldServer.Game.Maps.Pathfinding;
using System;
namespace Sunshine.WorldServer.Game.Maps
{
	public class MapCellsInformationProvider : ICellsInformationProvider
	{
		public Map Map
		{
			get;
			private set;
		}
		public MapCellsInformationProvider(Map map)
		{
			this.Map = map;
		}
		public bool IsCellWalkable(short cell)
		{
			return this.Map.Cells[(int)cell].Walkable && !this.Map.Cells[(int)cell].NonWalkableDuringRP;
		}
		public CellInformation GetCellInformation(short cell)
		{
			return new CellInformation(cell, this.IsCellWalkable(cell));
		}
	}
}
