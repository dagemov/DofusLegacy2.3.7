using Sunshine.Logs;
using Sunshine.MySql.Database.Managers;
using Sunshine.Protocol.Enums;
using Sunshine.Servers;
using Sunshine.WorldServer.Game.Characters;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sunshine.BaseServer.Shutdown
{
    public static class ShutdownManager
    {
        private static readonly object SyncRoot = new object();
        private static CancellationTokenSource _shutdownCts;
        private static DateTime? _shutdownAtUtc;

        public static bool Schedule(int seconds, out string result)
        {
            if (seconds <= 0)
            {
                result = "Le délai doit être supérieur à 0 seconde.";
                return false;
            }

            CancellationTokenSource cts;
            lock (SyncRoot)
            {
                if (_shutdownCts != null)
                    _shutdownCts.Cancel();

                _shutdownCts = new CancellationTokenSource();
                _shutdownAtUtc = DateTime.UtcNow.AddSeconds(seconds);
                cts = _shutdownCts;
            }

            result = $"Arrêt planifié dans {seconds} seconde(s).";

            Task.Run(async () =>
            {
                try
                {
                    await CountdownAsync(seconds, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    Broadcast("<b>[SHUTDOWN]</b> Canceled !", Color.OrangeRed);
                    Logger.WriteInfo("Scheduled shutdown cancelled.");
                }
                finally
                {
                    lock (SyncRoot)
                    {
                        if (_shutdownCts == cts)
                        {
                            _shutdownCts.Dispose();
                            _shutdownCts = null;
                            _shutdownAtUtc = null;
                        }
                    }
                }
            });

            return true;
        }

        public static bool Cancel(out string result)
        {
            lock (SyncRoot)
            {
                if (_shutdownCts == null)
                {
                    result = "Aucun arrêt serveur n'est planifié.";
                    return false;
                }

                _shutdownCts.Cancel();
                ServersManager.Instance.UpdateServerStatus(ServerStatusEnum.ONLINE);
                result = "Arrêt serveur annulé.";
                return true;
            }
        }

        private static async Task CountdownAsync(int totalSeconds, CancellationToken token)
        {
            Logger.WriteInfo($"Scheduled shutdown in {totalSeconds} second(s).");
            ServersManager.Instance.UpdateServerStatus(ServerStatusEnum.STOPING);

            for (int remaining = totalSeconds; remaining > 0; remaining--)
            {
                token.ThrowIfCancellationRequested();

                if (ShouldBroadcast(remaining))
                    BroadcastOfficialCountdown(remaining);

                await Task.Delay(1000, token);
            }

            try
            {
                foreach (var character in CharacterManager.Instance.Characters.Values
                             .Where(x => x != null && x.Client != null && x.IsInWorld())
                             .ToList())
                {
                    try
                    {
                        // Ajouter ici la logique éventuelle avant fermeture joueur
                    }
                    catch
                    {
                    }
                }

                try
                {
                    ServersManager.Instance.Save();
                }
                catch (Exception ex)
                {
                    Logger.WriteError("Shutdown save failed: " + ex);
                }

                await Task.Delay(1000, token);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static void BroadcastOfficialCountdown(int remainingSeconds)
        {
            string formatted = TimeSpan.FromSeconds(Math.Max(0, remainingSeconds)).ToString(@"mm\:ss");

            foreach (var character in CharacterManager.Instance.Characters.Values
                         .Where(x => x != null && x.Client != null && x.IsInWorld())
                         .ToList())
            {
                try
                {
                    character.SendInformationMessage(
                        TextInformationTypeEnum.TEXT_INFORMATION_ERROR,
                        15,
                        formatted);
                }
                catch
                {
                }
            }
        }

        private static bool ShouldBroadcast(int remaining)
        {
            return remaining == 1 ||
                   remaining == 2 ||
                   remaining == 3 ||
                   remaining == 4 ||
                   remaining == 5 ||
                   remaining == 10 ||
                   remaining == 15 ||
                   remaining == 20 ||
                   remaining == 30 ||
                   remaining == 45 ||
                   remaining == 60 ||
                   remaining % 300 == 0;
        }

        private static void Broadcast(string message, Color color)
        {
            foreach (var character in CharacterManager.Instance.Characters.Values
                         .Where(x => x != null && x.Client != null && x.IsInWorld())
                         .ToList())
            {
                try
                {
                    character.SendServerMessage(message, color);
                }
                catch
                {
                }
            }
        }
    }
}