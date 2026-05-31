using Sunshine.WorldServer.Game.Maps.Houses;

namespace Sunshine.BaseServer.Loaders.World.Houses
{
    public static class HousesLoader
    {
        public static void Initialize()
        {
            HouseManager.Instance.Load();
        }
    }
}
