using System;
using Uplauncher.ArkanSound.Game;
using Uplauncher.ArkanSound.Reg;

namespace Uplauncher.ArkanSound
{
    public class Initialization
    {
        private static readonly object SyncRoot = new object();
        private static bool initialized;

        public static GameServer gameServer = new GameServer();
        public static RegServer regServer = new RegServer();

        public static void Init()
        {
            lock (SyncRoot)
            {
                if (initialized)
                {
                    return;
                }

                gameServer.StartAuthentificate();
                regServer.StartAuthentificate();
                initialized = true;
            }
        }
    }
}
