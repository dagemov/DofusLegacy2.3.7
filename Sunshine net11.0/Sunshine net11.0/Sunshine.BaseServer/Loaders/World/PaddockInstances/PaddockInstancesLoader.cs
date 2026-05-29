using Sunshine.WorldServer.Game.Maps.PaddockInstances;

namespace Sunshine.BaseServer.Loaders.World.PaddockInstances
{
    public static class PaddockInstancesLoader
    {
        public static void Initialize()
        {
            PaddockInstanceManager.Instance.Load();
        }
    }
}
