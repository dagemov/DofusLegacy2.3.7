using Sunshine.WorldServer.Game.Maps.Paddocks;

namespace Sunshine.BaseServer.Loaders.World.Paddocks
{
    public static class PaddocksLoader
    {
        public static void Initialize()
        {
            PaddockManager.Instance.Load();
        }
    }
}
