using System;
using Sunshine.Logs;

namespace Sunshine.BaseServer.Configuration
{
    public sealed class ServerRatesProvider : IServerRatesProvider
    {
        private static readonly object SyncRoot = new object();
        private static IServerRatesProvider _instance = new ServerRatesProvider();

        private readonly ServerRatesConfigLoader _loader = new ServerRatesConfigLoader();
        private ServerRatesConfig _current = ServerRatesConfig.CreateSafeDefaults();
        private string _loadedFilePath;

        public static IServerRatesProvider Instance
        {
            get => _instance;
            set => _instance = value ?? new ServerRatesProvider();
        }

        public string LoadedFilePath => _loadedFilePath;

        public ServerRatesConfig Current
        {
            get
            {
                lock (SyncRoot)
                {
                    return _current;
                }
            }
        }

        public void Reload(string filePath = null)
        {
            lock (SyncRoot)
            {
                var result = _loader.Load(
                    filePath ?? ServerRatesConfigLoader.ResolveDefaultPath(),
                    Logger.WriteInfo,
                    Logger.WriteWarning);

                _current = result.Config;
                _loadedFilePath = result.FilePath;

                Logger.WriteInfo($"Server rates loaded from '{result.FilePath}'");
                Logger.WriteInfo(
                    $"Server rates applied: XP_RATE={result.Config.XpRate}, DROP_RATE={result.Config.DropRate}, " +
                    $"KAMAS_RATE={result.Config.KamasRate}, PP_RATE={result.Config.PpRate}, " +
                    $"WEAPON_USES_PER_TURN={result.Config.WeaponUsesPerTurn}, " +
                    $"WEAPON_USES_PER_FIGHT={result.Config.WeaponUsesPerFight}, " +
                    $"SPELL_USES_DEFAULT={result.Config.SpellUsesDefault}");
            }
        }
    }
}
