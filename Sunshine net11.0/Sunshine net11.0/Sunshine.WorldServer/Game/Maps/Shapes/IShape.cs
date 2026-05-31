using Sunshine.Protocol.Enums;

namespace Sunshine.WorldServer.Game.Maps.Shapes
{
    public interface IShape
    {
        uint Surface
        {
            get;
        }

        byte MinRadius
        {
            get;
            set;
        }

        DirectionsEnum Direction
        {
            get;
            set;
        }

        byte Radius
        {
            get;
            set;
        }

        short[] GetCells(short centerCell, Map map);
    }
}