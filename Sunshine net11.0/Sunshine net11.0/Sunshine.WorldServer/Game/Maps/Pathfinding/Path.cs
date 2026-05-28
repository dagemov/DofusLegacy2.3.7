using Sunshine.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Sunshine.WorldServer.Game.Maps.Pathfinding
{
    public class Path
    {
        private short[] m_cellsPath;
        private MapPoint[] m_path;
        private ObjectPosition[] m_compressedPath;
        private ObjectPosition m_endPathPosition;
        public Map Map
        {
            get;
            private set;
        }
        public short StartCell
        {
            get
            {
                return this.m_cellsPath[0];
            }
        }
        public short EndCell
        {
            get
            {
                return this.m_cellsPath[this.m_cellsPath.Length - 1];
            }
        }
        public ObjectPosition EndPathPosition
        {
            get
            {
                ObjectPosition arg_2A_0;
                if ((arg_2A_0 = this.m_endPathPosition) == null)
                {
                    arg_2A_0 = (this.m_endPathPosition = new ObjectPosition(this.Map, this.EndCell, this.GetEndCellDirection()));
                }
                return arg_2A_0;
            }
        }
        public int MPCost
        {
            get
            {
                return this.m_cellsPath.Length - 1;
            }
        }
        public Path(Map map, System.Collections.Generic.IEnumerable<short> path)
        {
            this.Map = map;
            this.m_cellsPath = path.ToArray<short>();
            this.m_path = (
                from entry in this.m_cellsPath
                select new MapPoint(entry)).ToArray<MapPoint>();
        }
        private Path(Map map, System.Collections.Generic.IEnumerable<ObjectPosition> compressedPath)
        {
            this.Map = map;
            this.m_compressedPath = compressedPath.ToArray<ObjectPosition>();
            this.m_cellsPath = this.BuildCompletePath();
            this.m_path = (
                from entry in this.m_cellsPath
                select new MapPoint(entry)).ToArray<MapPoint>();
        }
        public bool IsEmpty()
        {
            return this.m_cellsPath.Length == 0;
        }
        public DirectionsEnum GetEndCellDirection()
        {
            DirectionsEnum result;
            if (this.m_cellsPath.Length <= 1)
            {
                result = DirectionsEnum.DIRECTION_EAST;
            }
            else
            {
                if (this.m_compressedPath != null)
                {
                    result = this.m_compressedPath.Last<ObjectPosition>().Direction;
                }
                else
                {
                    result = this.m_path[this.m_path.Length - 2].OrientationToAdjacent(this.m_path[this.m_path.Length - 1]);
                }
            }
            return result;
        }
        public ObjectPosition[] GetCompressedPath()
        {
            ObjectPosition[] arg_19_0;
            if ((arg_19_0 = this.m_compressedPath) == null)
            {
                arg_19_0 = (this.m_compressedPath = this.BuildCompressedPath());
            }
            return arg_19_0;
        }
        public short[] GetPath()
        {
            return this.m_cellsPath;
        }
        public bool Contains(short cellId)
        {
            return this.m_cellsPath.Any((short entry) => entry == cellId);
        }
        public System.Collections.Generic.IEnumerable<short> GetServerPathKeys()
        {
            return
                from entry in this.m_cellsPath
                select entry;
        }
        public void CutPath(int index)
        {
            if (index <= this.m_cellsPath.Length - 1)
            {
                this.m_cellsPath = this.m_cellsPath.Take(index).ToArray<short>();
                this.m_path = (
                    from entry in this.m_cellsPath
                    select new MapPoint(entry)).ToArray<MapPoint>();
                this.m_endPathPosition = new ObjectPosition(this.Map, this.EndCell, this.GetEndCellDirection());
            }
        }
        private ObjectPosition[] BuildCompressedPath()
        {
            ObjectPosition[] result;
            if (this.m_cellsPath.Length <= 0)
            {
                result = new ObjectPosition[0];
            }
            else
            {
                if (this.m_cellsPath.Length <= 1)
                {
                    result = new ObjectPosition[]
                    {
                        new ObjectPosition(this.Map, this.m_cellsPath[0])
                    };
                }
                else
                {
                    System.Collections.Generic.List<ObjectPosition> list = new System.Collections.Generic.List<ObjectPosition>();
                    for (int i = 1; i < this.m_cellsPath.Length; i++)
                    {
                        list.Add(new ObjectPosition(this.Map, this.m_cellsPath[i - 1], this.m_path[i - 1].OrientationToAdjacent(this.m_path[i])));
                    }
                    list.Add(new ObjectPosition(this.Map, this.m_cellsPath[this.m_cellsPath.Length - 1], list[list.Count - 1].Direction));
                    if (list.Count > 0)
                    {
                        for (int i = list.Count - 2; i > 0; i--)
                        {
                            if (list[i].Direction == list[i - 1].Direction)
                            {
                                list.RemoveAt(i);
                            }
                        }
                    }
                    result = list.ToArray();
                }
            }
            return result;
        }
        private short[] BuildCompletePath()
        {
            System.Collections.Generic.List<short> list = new System.Collections.Generic.List<short>();
            for (int i = 0; i < this.m_compressedPath.Length - 1; i++)
            {
                list.Add(this.m_compressedPath[i].Cell);
                int num = 0;
                MapPoint mapPoint = this.m_compressedPath[i].Point;
                while ((mapPoint = mapPoint.GetNearestCellInDirection(this.m_compressedPath[i].Direction)) != null && mapPoint.CellId != this.m_compressedPath[i + 1].Cell)
                {
                    if ((long)num > 54L)
                    {
                        throw new System.Exception("Path too long. Maybe an orientation problem ?");
                    }
                    list.Add(this.Map.Cells[(int)mapPoint.CellId].Id);
                    num++;
                }
            }
            list.Add(this.m_compressedPath[this.m_compressedPath.Length - 1].Cell);
            return list.ToArray();
        }
        public static Path BuildFromCompressedPath(Map map, System.Collections.Generic.IEnumerable<short> keys)
        {
            System.Collections.Generic.IEnumerable<ObjectPosition> compressedPath =
                from key in keys
                let cellId = (int)(key & 4095)
                let direction = (DirectionsEnum)(key >> 12 & 7)
                select new ObjectPosition(map, map.Cells[cellId].Id, direction);
            return new Path(map, compressedPath);
        }
        public static Path GetEmptyPath(Map map, short startCell)
        {
            return new Path(map, new short[]
            {
                startCell
            });
        }
    }
}
