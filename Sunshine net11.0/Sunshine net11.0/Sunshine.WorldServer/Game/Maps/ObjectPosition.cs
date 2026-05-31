using Sunshine.Protocol.Enums;
using Sunshine.Protocol.Tools.Dlm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sunshine.WorldServer.Game.Maps
{
    public class ObjectPosition
    {
        public Map Map { get; set; }

        public short Cell { get; set; }

        public DirectionsEnum Direction { get; set; }

        public MapPoint Point { get { return new MapPoint(Cell); } }

        public DlmCellData CellData { get; }

        public ObjectPosition(short cell, DirectionsEnum direction)
        {
            CellData = new DlmCellData(cell);
            Cell = cell;
            Direction = direction;
        }

        public ObjectPosition(Map map, short cell, DirectionsEnum direction)
        {
            Map = map;
            Cell = cell;
            Direction = direction;
        }

        public ObjectPosition(Map map, short cell)
        {
            Map = map;
            Cell = cell;
        }
    }
}
