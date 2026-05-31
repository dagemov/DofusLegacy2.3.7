using Sunshine.Protocol.Enums;
using Sunshine.WorldServer.Game.Maps;
using System;
using System.Collections.Generic;

namespace Sunshine.WorldServer.Game.Maps.Shapes
{
    public class Square : IShape
    {
        public Square(byte minRadius, byte radius)
        {
            MinRadius = minRadius;
            Radius = radius;
        }

        public bool DiagonalFree
        {
            get;
            set;
        }

        #region IShape Members

        public uint Surface
        {
            get
            {
                return ( (uint)Radius * 2 + 1 ) * ( (uint)Radius * 2 + 1 );
            }
        }

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

        public short[] GetCells(short centerCell, Map map)
        {
            var centerPoint = new MapPoint(centerCell);
            var result = new List<short>();

            if (Radius == 0)
            {
                if (MinRadius == 0 && !DiagonalFree)
                    result.Add(centerCell);

                return result.ToArray();
            }

            int x = (int)( centerPoint.X - Radius );
            int y;
            while (x <= centerPoint.X + Radius)
            {
                y = (int) (centerPoint.Y - Radius);
                while (y <= centerPoint.Y + Radius)
                {
                    if (MinRadius == 0 || Math.Abs(centerPoint.X - x) + Math.Abs(centerPoint.Y - y) >= MinRadius)
                        if (!DiagonalFree || Math.Abs(centerPoint.X - x) != Math.Abs(centerPoint.Y - y))
                             AddCellIfValid(x, y, map, result);

                    y++;
                }

                x++;
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