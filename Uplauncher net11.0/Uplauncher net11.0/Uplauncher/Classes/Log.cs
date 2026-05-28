using System;
using System.IO;
using System.Text;

namespace Uplauncher.Classes
{
    public class Log
    {
        public static void PublishLog(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(".\\SunshineUplauncher.txt", true, Encoding.UTF8))
                {
                    writer.WriteLine("[{0}] :: {1}", DateTime.Now, message);
                }
            }
            catch
            {
            }
        }

        public static void PublishWarning(string msg)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(".\\SunshineUplauncher.txt", true, Encoding.UTF8))
                {
                    writer.WriteLine("[{0}] :: {1}", DateTime.Now, msg);
                }
            }
            catch
            {
            }
        }

        public static void PublishExceptionLog(Exception e)
        {
            Log.PublishLog(string.Format("{0} threw an unhandled exception: {1} => {2}", e.TargetSite, e.Message, e.StackTrace));
        }

        public static void PublishExceptionWarning(Exception e, bool show_message = true)
        {
            Log.PublishWarning(string.Format("{0} threw an exception: {1} => {2}", e.TargetSite, e.Message, e.StackTrace));
        }

        public static void PublishExceptionWarningText(Exception e, string text, bool show_message = true)
        {
            Log.PublishWarning(string.Format("{0} threw an exception: {1} => {2}", e.TargetSite, e.Message, e.StackTrace));
            if (!string.IsNullOrWhiteSpace(text))
            {
                Log.PublishWarning(text);
            }
        }
    }
}
