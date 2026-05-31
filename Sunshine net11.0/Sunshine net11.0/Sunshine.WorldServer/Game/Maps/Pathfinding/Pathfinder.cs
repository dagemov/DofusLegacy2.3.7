using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Sunshine.Protocol.Enums;
using Sunshine.Protocol.IO.Collections;

namespace Sunshine.WorldServer.Game.Maps.Pathfinding
{
	public class Pathfinder
	{
		internal class ComparePfNodeMatrix : System.Collections.Generic.IComparer<short>
		{
			private readonly PathNode[] m_matrix;
			public ComparePfNodeMatrix(PathNode[] matrix)
			{
				this.m_matrix = matrix;
			}
			public int Compare(short a, short b)
			{
				int result;
				if (this.m_matrix[(int)a].F > this.m_matrix[(int)b].F)
				{
					result = 1;
				}
				else
				{
					if (this.m_matrix[(int)a].F < this.m_matrix[(int)b].F)
					{
						result = -1;
					}
					else
					{
						result = 0;
					}
				}
				return result;
			}
		}
		public static int SearchLimit = 500;
		public static int EstimateHeuristic = 1;
		private static readonly DirectionsEnum[] Directions;
		public ICellsInformationProvider CellsInformationProvider
		{
			get;
			private set;
		}
		private static double GetHeuristic(MapPoint pointA, MapPoint pointB)
		{
			Point point = new Point(System.Math.Abs(pointB.X - pointA.X), System.Math.Abs(pointB.Y - pointA.Y));
			int num = System.Math.Abs(point.X - point.Y);
			int num2 = System.Math.Abs((point.X + point.Y - num) / 2);
			return (double)(Pathfinder.EstimateHeuristic * (num2 + num + point.X + point.Y));
		}
		public Pathfinder(ICellsInformationProvider cellsInformationProvider)
		{
			this.CellsInformationProvider = cellsInformationProvider;
		}
        public Path FindPath(short startCell, short endCell, bool diagonal, int movementPoints = -1)
        {
            bool flag = false;
            PathNode[] matrix = new PathNode[561];
            PriorityQueueB<short> priorityQueueB = new PriorityQueueB<short>((IComparer<short>)new Pathfinder.ComparePfNodeMatrix(matrix));
            List<PathNode> list = new List<PathNode>();
            MapPoint mapPoint1 = new MapPoint(startCell);
            MapPoint pointB = new MapPoint(endCell);
            short num1 = startCell;
            int num2 = 0;
            Path path;
            if (movementPoints == 0)
            {
                path = Path.GetEmptyPath(this.CellsInformationProvider.Map, this.CellsInformationProvider.Map.Cells[(int)startCell].Id);
            }
            else
            {
                matrix[(int)num1].Cell = num1;
                matrix[(int)num1].Parent = (short)-1;
                matrix[(int)num1].G = 0.0;
                matrix[(int)num1].F = (double)Pathfinder.EstimateHeuristic;
                matrix[(int)num1].Status = NodeState.Open;
                priorityQueueB.Push(num1);
                while (priorityQueueB.Count > 0)
                {
                    short cellId1 = priorityQueueB.Pop();
                    MapPoint mapPoint2 = new MapPoint(cellId1);
                    if (matrix[(int)cellId1].Status != NodeState.Closed)
                    {
                        if ((int)cellId1 != (int)endCell)
                        {
                            if (num2 <= Pathfinder.SearchLimit)
                            {
                                for (int index = 0; index < (!diagonal ? 4 : 8); ++index)
                                {
                                    MapPoint nearestCellInDirection = mapPoint2.GetNearestCellInDirection(Pathfinder.Directions[index]);
                                    if (nearestCellInDirection != null)
                                    {
                                        short cellId2 = nearestCellInDirection.CellId;
                                        if (((int)cellId2 < 0 ? 0 : ((long)cellId2 < 560L ? 1 : 0)) != 0 && MapPoint.IsInMap(nearestCellInDirection.X, nearestCellInDirection.Y) && this.CellsInformationProvider.IsCellWalkable(cellId2))
                                        {
                                            double num3 = matrix[(int)cellId1].G + 1.0;
                                            if ((matrix[(int)cellId2].Status == NodeState.Open || matrix[(int)cellId2].Status == NodeState.Closed ? (matrix[(int)cellId2].G > num3 ? 1 : 0) : 1) != 0)
                                            {
                                                matrix[(int)cellId2].Cell = cellId2;
                                                matrix[(int)cellId2].Parent = cellId1;
                                                matrix[(int)cellId2].G = num3;
                                                matrix[(int)cellId2].H = Pathfinder.GetHeuristic(nearestCellInDirection, pointB);
                                                matrix[(int)cellId2].F = num3 + matrix[(int)cellId2].H;
                                                priorityQueueB.Push(cellId2);
                                                matrix[(int)cellId2].Status = NodeState.Open;
                                            }
                                        }
                                    }
                                }
                                ++num2;
                                matrix[(int)cellId1].Status = NodeState.Closed;
                            }
                            else
                            {
                                path = Path.GetEmptyPath(this.CellsInformationProvider.Map, this.CellsInformationProvider.Map.Cells[(int)startCell].Id);
                                goto label_23;
                            }
                        }
                        else
                        {
                            matrix[(int)cellId1].Status = NodeState.Closed;
                            flag = true;
                            break;
                        }
                    }
                }
                if (flag)
                {
                    PathNode pathNode;
                    for (pathNode = matrix[(int)endCell]; (int)pathNode.Parent != -1; pathNode = matrix[(int)pathNode.Parent])
                        list.Add(pathNode);
                    list.Add(pathNode);
                }
                list.Reverse();
                path = (movementPoints <= 0 ? 1 : (list.Count + 1 <= movementPoints ? 1 : 0)) != 0 ? new Path(this.CellsInformationProvider.Map, Enumerable.Select<PathNode, short>((IEnumerable<PathNode>)list, (Func<PathNode, short>)(entry => this.CellsInformationProvider.Map.Cells[(int)entry.Cell].Id))) : new Path(this.CellsInformationProvider.Map, Enumerable.Select<PathNode, short>(Enumerable.Take<PathNode>((IEnumerable<PathNode>)list, movementPoints + 1), (Func<PathNode, short>)(entry => this.CellsInformationProvider.Map.Cells[(int)entry.Cell].Id)));
            }
        label_23:
            return path;
        }
		static Pathfinder()
		{
			// Note: this type is marked as 'beforefieldinit'.
			DirectionsEnum[] array = new DirectionsEnum[8];
			array[0] = DirectionsEnum.DIRECTION_SOUTH_WEST;
			array[1] = DirectionsEnum.DIRECTION_NORTH_WEST;
			array[2] = DirectionsEnum.DIRECTION_NORTH_EAST;
			array[3] = DirectionsEnum.DIRECTION_SOUTH_EAST;
			array[4] = DirectionsEnum.DIRECTION_SOUTH;
			array[5] = DirectionsEnum.DIRECTION_WEST;
			array[6] = DirectionsEnum.DIRECTION_NORTH;
			Pathfinder.Directions = array;
		}
	}
}
