using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Maps;
using System.Collections.Generic;
using Sunshine.Protocol.Utils.Extensions;

namespace Sunshine.WorldServer.Game.Maps.Shapes
{
    public class Line : IShape
    {
        public Line(byte radius, bool opposedDirection)
        {
            Radius = radius;
            Direction = DirectionsEnum.DIRECTION_SOUTH_EAST;
            OpposedDirection = opposedDirection;
        }

        #region IShape Members

        public uint Surface => (uint)Radius + 1;

        public byte MinRadius
        {
            get;
            set;
        }

        public DirectionsEnum Direction
        {
            get;
            set;
        }

        public byte Radius
        {
            get;
            set;
        }

        public bool OpposedDirection
        {
            get;
            set;
        }

        public short[] GetCells(short centerCell, Map map)
        {
            if (OpposedDirection)
                Direction = Direction.GetOpposedDirection();

            var centerPoint = new MapPoint(centerCell);
            var result = new List<short>();

            for (int i = MinRadius; i <= Radius; i++)
            {
                switch (Direction)
                {
                    case DirectionsEnum.DIRECTION_WEST:
                        AddCellIfValid(centerPoint.X - i, centerPoint.Y - i, map, result);
                        break;
                    case DirectionsEnum.DIRECTION_NORTH:
                        AddCellIfValid(centerPoint.X - i, centerPoint.Y + i, map, result);
                        break;
                    case DirectionsEnum.DIRECTION_EAST:
                        AddCellIfValid(centerPoint.X + i, centerPoint.Y + i, map, result);
                        break;
                    case DirectionsEnum.DIRECTION_SOUTH:
                        AddCellIfValid(centerPoint.X + i, centerPoint.Y - i, map, result);
                        break;
                    case DirectionsEnum.DIRECTION_NORTH_WEST:
                        AddCellIfValid(centerPoint.X - i, centerPoint.Y, map, result);
                        break;
                    case DirectionsEnum.DIRECTION_SOUTH_WEST:
                        AddCellIfValid(centerPoint.X, centerPoint.Y - i, map, result);
                        break;
                    case DirectionsEnum.DIRECTION_SOUTH_EAST:
                        AddCellIfValid(centerPoint.X + i, centerPoint.Y, map, result);
                        break;
                    case DirectionsEnum.DIRECTION_NORTH_EAST:
                        AddCellIfValid(centerPoint.X, centerPoint.Y + i, map, result);
                        break;
                }
            }

            return result.ToArray();
        }

        private static void AddCellIfValid(int x, int y, Map map, IList<short> container)
        {
            if (!MapPoint.IsInMap(x, y))
                return;

            container.Add(map.Cells[MapPoint.CoordToCellId(x, y)].Id);
        }
        #endregion
    }
}