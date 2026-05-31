using System;
namespace Sunshine.WorldServer.Game.Maps.Pathfinding
{
	public struct PathNode
	{
		public short Cell;
		public double F;
		public double G;
		public double H;
		public short Parent;
		public NodeState Status;
	}
}
