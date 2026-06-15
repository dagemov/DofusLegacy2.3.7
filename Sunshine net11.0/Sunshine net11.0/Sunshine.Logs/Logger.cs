using System;
using System.IO;

namespace Sunshine.Logs
{
    public class Logger
    {
        public static bool EnableDiskLogs = false;
        public static bool EnablePacketLogs = false;

        public static void Write(string text)
        {
            Program.WriteConsoleLine(text, ConsoleColor.White);
        }

        public static void WriteInfo(string info)
        {
            string message = string.Format("[ Info ] {0}", info);
            Program.WriteConsoleLine(message, ConsoleColor.Green);

            if (EnableDiskLogs)
                SaveLog(message);
        }

        public static void WriteWarning(string warning)
        {
            string message = string.Format("[ Warning ] {0}", warning);
            Program.WriteConsoleLine(message, ConsoleColor.Yellow);

            if (EnableDiskLogs)
                SaveLog(message);
        }

        public static void WriteError(string error)
        {
            string message = string.Format("[ Error ] {0}", error);
            Program.WriteConsoleLine(message, ConsoleColor.Red);
            SaveLog(message, true);
        }

        public static void WriteClient(string clt, string ip, int port, string message)
        {
            if (!EnablePacketLogs)
                return;

            string text = string.Format("[ {0} ] <{2}:{3}> Receive {1}", clt, message, ip, port);
            Program.WriteConsoleLine(text, ConsoleColor.Cyan);
            SaveLog(text);
        }

        public static void WriteServer(string srv, string ip, int port, string message)
        {
            if (!EnablePacketLogs)
                return;

            string text = string.Format("[ {0} ] <{2}:{3}> Send {1}", srv, message, ip, port);
            Program.WriteConsoleLine(text, ConsoleColor.Cyan);
            SaveLog(text);
        }

        private static void SaveLog(string message, bool error = false)
        {
            try
            {
                string date = DateTime.Now.ToShortDateString().Replace('/', '-');
                string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                string path = error
                    ? Path.Combine(logsDirectory, string.Format("errorlog_{0}.txt", date))
                    : Path.Combine(logsDirectory, string.Format("log_{0}.txt", date));

                Directory.CreateDirectory(logsDirectory);
                using (var logs = new StreamWriter(path, true))
                {
                    logs.WriteLine(message);
                }
            }
            catch
            {
                return;
            }
        }
    }
}
