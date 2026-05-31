using System;
using System.Threading;
using System.Windows;
using Uplauncher.Classes;

namespace Uplauncher
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string[] arguments = new string[0];
        private static Mutex singleInstanceMutex;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            string[] args = e.Args ?? new string[0];
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "-wait", StringComparison.OrdinalIgnoreCase))
                {
                    Thread.Sleep(1000);
                    break;
                }
            }

            bool createdNew;
            singleInstanceMutex = new Mutex(true, "SunshineUplauncher_SingleInstance", out createdNew);
            if (!createdNew)
            {
                Shutdown();
                return;
            }

            App.arguments = args;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            base.OnStartup(e);
        }

        internal static void ReleaseSingleInstanceMutex()
        {
            try
            {
                if (singleInstanceMutex != null)
                {
                    singleInstanceMutex.ReleaseMutex();
                    singleInstanceMutex.Dispose();
                    singleInstanceMutex = null;
                }
            }
            catch
            {
            }
        }

        internal static bool RecreateSingleInstanceMutex()
        {
            try
            {
                if (singleInstanceMutex != null)
                {
                    return true;
                }

                bool createdNew;
                singleInstanceMutex = new Mutex(true, "SunshineUplauncher_SingleInstance", out createdNew);
                return createdNew;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ReleaseSingleInstanceMutex();
            base.OnExit(e);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                Log.PublishWarning("Unhandled launcher exception: " + exception);
            }
            else if (e.ExceptionObject != null)
            {
                Log.PublishWarning("Unhandled launcher exception: " + e.ExceptionObject);
            }
        }
    }
}
