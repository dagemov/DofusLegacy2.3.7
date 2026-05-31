using Sunshine.Protocol.Enums;

namespace Sunshine.WorldServer.Game.Maps.Shapes
{ 
    public class Single : IShape
    {
        public uint Surface
        {
            get
            {
                return 1;
            }
        }

        public byte MinRadius
        {
            get
            {
                return 1;
            }
            set { }
        }

        public DirectionsEnum Direction
        {
            get
            {
                return DirectionsEnum.DIRECTION_NORTH; 
            }
            set { }
        }

        public byte Radius
        {
            get { return 1; }
            set {  }
        }

        public short[] GetCells(short centerCell, Map map)
        {
            return new [] {centerCell};
        }
    }
}