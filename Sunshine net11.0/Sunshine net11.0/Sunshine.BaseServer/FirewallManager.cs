using Sunshine.Logs;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace Sunshine.BaseServer
{
    public static class FirewallManager
    {
        private const string RuleAuth = "Sunshine AuthServer TCP";
        private const string RuleWorld = "Sunshine WorldServer TCP";

        public static void EnsureFirewallRules(int authPort, int worldPort)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                    return;

                string exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                    return;

                string commands = BuildNetshCommands(exePath, authPort, worldPort);

                if (IsAdministrator())
                {
                    RunHidden("cmd.exe", "/c " + commands, false);
                    Logger.WriteInfo("Règles pare-feu vérifiées/créées pour Sunshine.");
                    return;
                }

                Logger.WriteInfo("Droits administrateur requis pour créer les règles pare-feu. Demande UAC...");
                RunHidden("cmd.exe", "/c " + commands, true);
            }
            catch (Exception ex)
            {
                Logger.WriteError("Impossible de configurer automatiquement le pare-feu : " + ex.Message);
                Logger.WriteError("Lance Sunshine en administrateur ou ajoute manuellement les ports TCP 446 et 3467 dans le Pare-feu Windows.");
            }
        }

        private static string BuildNetshCommands(string exePath, int authPort, int worldPort)
        {
            string app = Quote(exePath);

            return
                "netsh advfirewall firewall delete rule name=" + Quote(RuleAuth) + " >nul 2>&1 & " +
                "netsh advfirewall firewall delete rule name=" + Quote(RuleWorld) + " >nul 2>&1 & " +
                "netsh advfirewall firewall add rule name=" + Quote(RuleAuth) + " dir=in action=allow protocol=TCP localport=" + authPort + " program=" + app + " enable=yes profile=any >nul 2>&1 & " +
                "netsh advfirewall firewall add rule name=" + Quote(RuleWorld) + " dir=in action=allow protocol=TCP localport=" + worldPort + " program=" + app + " enable=yes profile=any >nul 2>&1 & " +
                // Compatibilité très vieux Windows : ancienne syntaxe netsh firewall.
                "netsh firewall set portopening TCP " + authPort + " " + Quote(RuleAuth) + " ENABLE >nul 2>&1 & " +
                "netsh firewall set portopening TCP " + worldPort + " " + Quote(RuleWorld) + " ENABLE >nul 2>&1";
        }

        private static void RunHidden(string fileName, string arguments, bool elevate)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = elevate;

                if (elevate)
                {
                    process.StartInfo.Verb = "runas";
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                }

                process.Start();

                if (!elevate)
                    process.WaitForExit(10000);
            }
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }
    }
}
