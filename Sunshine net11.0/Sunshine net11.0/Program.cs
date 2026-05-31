using Sunshine.BaseServer.Commands;
using Sunshine.Logs;
using Sunshine.Servers;
using Sunshine.BaseServer;
using Sunshine.AuthServer;
using Sunshine.WorldServer;
using System;
using System.Threading;
using System.Text;

namespace Sunshine
{
    public static class Program
    {
        private const bool DefaultShowConsoleLogs = true;
        private const bool DefaultShowLoadingScreen = true;
        private const bool DefaultInteractiveConsole = true;
        private const bool DefaultEnableDiskLogs = true;
        private const bool DefaultEnablePacketLogs = false;

        private static readonly object _consoleLock = new object();
        private static DateTime _startupStartedAtUtc;

        public static bool ShowConsoleLogs { get; private set; } = DefaultShowConsoleLogs;
        public static bool ShowLoadingScreen { get; private set; } = DefaultShowLoadingScreen;
        public static bool InteractiveConsoleEnabled { get; private set; } = DefaultInteractiveConsole;

        static void Main(string[] args)
        {
            BaseServer.Configuration.GameConfig.Load();
            BaseServer.Configuration.GameRates.Reload();
            ApplyStartupSettings(args);

            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Sunshine 2.3.7";
            Console.CursorVisible = false;

            StartConsoleBootJournal();

            FirewallManager.EnsureFirewallRules(AuthServer.AuthServer.Port, WorldServer.WorldServer.Port);

            ServersManager.Instance.Start();

            FinishLoading();
            Console.CursorVisible = true;

            if (InteractiveConsoleEnabled)
                RunCommandLoop();
            else
                Thread.Sleep(Timeout.Infinite);
        }

        private static void ApplyStartupSettings(string[] args)
        {
            ShowConsoleLogs = DefaultShowConsoleLogs;
            ShowLoadingScreen = DefaultShowLoadingScreen;
            InteractiveConsoleEnabled = DefaultInteractiveConsole;

            Logger.EnableDiskLogs = DefaultEnableDiskLogs;
            Logger.EnablePacketLogs = DefaultEnablePacketLogs;

            foreach (var arg in args ?? Array.Empty<string>())
            {
                var option = (arg ?? string.Empty).Trim().ToLowerInvariant();

                switch (option)
                {
                    case "--no-console-logs":
                    case "--silent":
                        ShowConsoleLogs = false;
                        break;

                    case "--no-loading":
                        ShowLoadingScreen = false;
                        break;

                    case "--no-console-input":
                        InteractiveConsoleEnabled = false;
                        break;

                    case "--disk-logs":
                        Logger.EnableDiskLogs = true;
                        break;

                    case "--packet-logs":
                        Logger.EnablePacketLogs = true;
                        break;
                }
            }
        }

        public static void WriteConsoleLine(string text, ConsoleColor color = ConsoleColor.White)
        {
            lock (_consoleLock)
            {
                var previousColor = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(text ?? string.Empty);
                }
                finally
                {
                    Console.ForegroundColor = previousColor;
                }
            }
        }

        private static void RunCommandLoop()
        {
            while (true)
            {
                var cmd = Console.ReadLine();
                if (cmd == null)
                    break;

                if (string.IsNullOrWhiteSpace(cmd))
                    continue;

                try
                {
                    BaseCommand.Execute(cmd);
                }
                catch (Exception ex)
                {
                    Logger.WriteError("Erreur commande console : " + ex.Message);
                }
            }
        }

        private static void StartConsoleBootJournal()
        {
            _startupStartedAtUtc = DateTime.UtcNow;

            lock (_consoleLock)
            {
                Console.Clear();
                RenderHeader();
                Console.WriteLine();
            }

            WriteSectionTitle("BOOT");
            WriteConsoleLine($"[BOOT] Démarrage de Sunshine le {DateTime.Now:dd/MM/yyyy HH:mm:ss}", ConsoleColor.Yellow);
            WriteConsoleLine($"[BOOT] Console logs      : {FormatEnabled(ShowConsoleLogs)}", ConsoleColor.DarkGray);
            WriteConsoleLine($"[BOOT] Journal chargement: {FormatEnabled(ShowLoadingScreen)}", ConsoleColor.DarkGray);
            WriteConsoleLine($"[BOOT] Console interactive: {FormatEnabled(InteractiveConsoleEnabled)}", ConsoleColor.DarkGray);
            WriteConsoleLine($"[BOOT] Logs disque       : {FormatEnabled(Logger.EnableDiskLogs)}", ConsoleColor.DarkGray);
            WriteConsoleLine($"[BOOT] Logs packets      : {FormatEnabled(Logger.EnablePacketLogs)}", ConsoleColor.DarkGray);
            WriteSeparator();
        }

        public static void BeginLoadingStep(int index, int total, string label, string detail)
        {
            if (!ShowLoadingScreen)
                return;

            WriteConsoleLine($"[LOAD {index,2}/{total}] [{GetPercent(index - 1, total),3}%] {label}", ConsoleColor.Cyan);

            if (!string.IsNullOrWhiteSpace(detail))
                WriteConsoleLine($"         ↳ {detail}", ConsoleColor.DarkGray);
        }

        public static void CompleteLoadingStep(int index, int total, string label, TimeSpan duration, string detail = null)
        {
            if (!ShowLoadingScreen)
                return;

            var durationText = FormatDuration(duration);
            var message = $"[ OK ] [{GetPercent(index, total),3}%] {label} chargé en {durationText}";
            WriteConsoleLine(message, ConsoleColor.Green);

            if (!string.IsNullOrWhiteSpace(detail))
                WriteConsoleLine($"       {detail}", ConsoleColor.DarkGray);
        }

        public static void FailLoadingStep(int index, int total, string label, Exception exception)
        {
            if (!ShowLoadingScreen)
                return;

            var reason = exception?.Message ?? "Erreur inconnue";
            WriteConsoleLine($"[FAIL] [{GetPercent(index - 1, total),3}%] {label} : {reason}", ConsoleColor.Red);
        }

        public static void FinishLoading()
        {
            var totalDuration = DateTime.UtcNow - _startupStartedAtUtc;

            if (ShowLoadingScreen)
            {
                WriteSeparator();
                WriteSectionTitle("READY");
                WriteConsoleLine($"[READY] Émulateur prêt en {FormatDuration(totalDuration)}.", ConsoleColor.Green);
                WriteConsoleLine("[READY] Les serveurs acceptent maintenant les connexions.", ConsoleColor.Green);
                WriteSeparator();
            }
            else
            {
                WriteConsoleLine($"Émulateur prêt en {FormatDuration(totalDuration)}.", ConsoleColor.Green);
            }
        }

        private static void RenderHeader()
        {
            string[] sunshineLogo =
            {
                @" ███████╗██╗   ██╗███╗   ██╗███████╗██╗  ██╗██╗███╗   ██╗███████╗",
                @" ██╔════╝██║   ██║████╗  ██║██╔════╝██║  ██║██║████╗  ██║██╔════╝",
                @" ███████╗██║   ██║██╔██╗ ██║███████╗███████║██║██╔██╗ ██║█████╗  ",
                @" ╚════██║██║   ██║██║╚██╗██║╚════██║██╔══██║██║██║╚██╗██║██╔══╝  ",
                @" ███████║╚██████╔╝██║ ╚████║███████║██║  ██║██║██║ ╚████║███████╗",
                @" ╚══════╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝╚══════╝",
                @"",
                @"                        •  2.3.7  •  .NET 10.0"
            };

            foreach (var line in sunshineLogo)
                WriteCenteredLine(line, line == string.Empty ? ConsoleColor.White : ConsoleColor.Yellow);
        }

        private static void WriteCenteredLine(string text, ConsoleColor color)
        {
            var width = Math.Max(Console.WindowWidth, text?.Length ?? 0);
            var padding = Math.Max(0, (width - (text?.Length ?? 0)) / 2);
            Console.ForegroundColor = color;
            Console.WriteLine(new string(' ', padding) + (text ?? string.Empty));
            Console.ResetColor();
        }

        private static void WriteSectionTitle(string title)
        {
            WriteConsoleLine($"== {title} ==", ConsoleColor.White);
        }

        private static void WriteSeparator()
        {
            var width = Math.Max(24, Math.Min(Console.WindowWidth - 1, 72));
            WriteConsoleLine(new string('─', width), ConsoleColor.DarkGray);
        }

        private static string FormatEnabled(bool enabled)
        {
            return enabled ? "ON" : "OFF";
        }

        private static int GetPercent(int index, int total)
        {
            if (total <= 0)
                return 100;

            var percent = (int)Math.Round((Math.Max(0, index) * 100d) / total);
            return Math.Max(0, Math.Min(100, percent));
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMilliseconds < 1000)
                return $"{Math.Max(0, (int)Math.Round(duration.TotalMilliseconds))} ms";

            return $"{duration.TotalSeconds:0.00} s";
        }
    }
}
