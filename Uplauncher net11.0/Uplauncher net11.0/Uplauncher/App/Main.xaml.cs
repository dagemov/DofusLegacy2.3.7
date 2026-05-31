using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;
using System.Xml.Linq;
using System.Net.Http;
using Uplauncher.Classes.Json;
using System.Reflection;
using Uplauncher.Classes;
using System.ComponentModel;
using Configuration = Uplauncher.Classes.Configuration;

namespace Uplauncher
{
    internal sealed class FlagItem
    {
        public string Code { get; set; }
        public string NormalImage { get; set; }
        public string HoverImage { get; set; }
    }

    internal sealed class NewsItem
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string DateText { get; set; }
        public string LogoImage { get; set; }
        public string Link { get; set; }
    }

    internal sealed class LogItem
    {
        public string Message { get; set; }
    }

    /// <summary>
    /// Logique d'interaction pour Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public enum CommandQueue
        {
            RunUpdater,
            VersionCheck,
            ServerUpdater,
            Relaunch
        }

        private UpdateWorker UpdateWorkerInstance { get; set; }
        public VersionCheckWorker VCheckWorkerInstance { get; set; }
        private Queue<CommandQueue> Commands = new Queue<CommandQueue>();
        public string CurrentLauncherFilename;
        private string CurrentLauncherPath;
        private bool _relaunchQueued;
        public string version;
        public string lang;
        public bool cor = false;
        public List<string> backgroundsList = new List<string>();
        public int currentBackgroundIndex;
        public bool ziperror = false;
        private IDialogManager dialogManager;
        public delegate void UpdateProgressDelegate(int percentage);
        private System.Windows.Forms.NotifyIcon MyNotifyIcon;
        private System.Windows.Forms.ToolStripMenuItem _trayOpenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _trayLaunchMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _trayQuitMenuItem;
        System.Timers.Timer timer = new System.Timers.Timer();
        private readonly string[] _flagCodes = { "BR", "DE", "EN", "ES", "FR", "IT", "JP", "NL", "PT", "RU", "US" };
        private readonly HashSet<string> _playButtonLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BR", "DE", "EN", "ES", "FR", "IT", "JA", "JP", "NL", "PT", "RU", "US"
        };
        private readonly HashSet<string> _menuButtonLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EN", "ES", "FR", "PT"
        };
        private bool _isUpdatingLanguageSelection;
        private bool _launcherReady;
        private const string DofusRssUrl = "http://sunshine-dofus.com/Uplauncher/rss.xml";
        private string _lastLogMessage = string.Empty;
        private int _lastDownloadLogMilestone = -1;

        #region Variables langs
        string text1;
        string text3;
        string text4;
        string text5;
        string text6;
        string text7;
        string text8;
        string text9;
        string text10;
        string text11;
        string text12;
        string text13;
        string text14;
        string text15;
        string text17;
        string text18;
        string text19;
        string text21;
        string text22;
        string text23;
        string text24;
        #endregion

        public Main()
        {
            this.LoadConfig();
            InitializeComponent();
            this.InitializeLanguageMenu();
            this.InitializeNewsAndLogs();

            MyNotifyIcon = new System.Windows.Forms.NotifyIcon();
            MyNotifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            MyNotifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(MyNotifyIcon_MouseDoubleClick);
            this.InitializeNotifyIconMenu();

            this.LoadBackgrounds();
            this.ShowVersionCheckButton();
            this.playButton.Visibility = Visibility.Visible;
            this._launcherReady = false;
            gerarradio();
            if (cor == true)
                cor = false;
            else
                cor = true;
            checkcor();
            checklang();
            this.RefreshWindowFrameButtonsIdleState();
            this.RefreshPlayButtonIdleState();
            this.RefreshVersionCheckButtonIdleState();
            this.ShowLanguageMenu(false);
            this.ShowNewsTab();
            textState.Content = text13;
            this.HideUpdateStatusDisplay();

            timer.Interval = 7000;
            timer.Elapsed += this.ChangeBackground;
            timer.AutoReset = true;
            timer.Enabled = true;

            this.UpdateWorkerInstance = new UpdateWorker();
            this.UpdateWorkerInstance.ProgressChanged += new ProgressChangedEventHandler(this.UpdateWorkerInstance_ProgressChanged);
            this.UpdateWorkerInstance.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.UpdateWorkerInstance_RunWorkerCompleted);
            this.dialogManager = new DialogManager(this, base.Dispatcher);
            this.Commands.Enqueue(CommandQueue.RunUpdater);

            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = assembly.GetName();
            string purefilename = assemblyName.Name;
            if (string.IsNullOrWhiteSpace(purefilename))
            {
                purefilename = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
            }
            if (purefilename.EndsWith("_d", StringComparison.OrdinalIgnoreCase) && purefilename.Length > 2)
            {
                purefilename = purefilename.Substring(0, purefilename.Length - 2);
            }

            this.CurrentLauncherFilename = purefilename;
            this.CurrentLauncherPath = Path.Combine(AppContext.BaseDirectory, this.CurrentLauncherFilename + ".exe");
            if (!File.Exists(this.CurrentLauncherPath))
            {
                this.CurrentLauncherPath = Path.Combine(Directory.GetCurrentDirectory(), this.CurrentLauncherFilename + ".exe");
            }

            this.ProcessQueueCommand();
        }

        private void UpdateWorkerInstance_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.Commands.Clear();
                this._relaunchQueued = false;
                Log.PublishExceptionLog(e.Error);
                this.FinishProgress();
                this.Status(text22);
                this.AddLogEntry(text23 + " " + e.Error.Message);
                this.ShowLogsTab();
                return;
            }

            if (!this.UpdateWorkerInstance.Cancel)
            {
                this.Commands.Enqueue(CommandQueue.ServerUpdater);
            }

            this.ProcessQueueCommand();
        }

        private void ProcessQueueCommand()
        {
            if (this.Commands.Count != 0)
            {
                CommandQueue command = this.Commands.Dequeue();
                this.ExecuteQueueCommand(command);
            }
        }

        public void ForceCommand(CommandQueue command)
        {
            this.ExecuteQueueCommand(command);
        }

        private void ExecuteQueueCommand(CommandQueue command)
        {
            switch (command)
            {
                case CommandQueue.RunUpdater:
                    this._launcherReady = false;
                    this.playButton.Visibility = Visibility.Visible;
                    this.RefreshPlayButtonIdleState();
                    this.RefreshVersionCheckButtonWaitingState();
                    this.UpdateWorkerInstance.Cancel = false;
                    this.UpdateWorkerInstance.RunWorkerAsync();
                    break;

                case CommandQueue.Relaunch:
                    this.Relaunch();
                    break;
                case CommandQueue.VersionCheck:
                    this._launcherReady = false;
                    this.playButton.Visibility = Visibility.Visible;
                    this.RefreshPlayButtonIdleState();
                    this.RefreshVersionCheckButtonWaitingState();
                    if (!this.UpdateWorkerInstance.IsBusy)
                    {
                        this.UpdateWorkerInstance.Cancel = false;
                        this.UpdateWorkerInstance.RunWorkerAsync();
                    }
                    break;
            }
        }

        private void Relaunch()
        {
            string launcherPath = this.ResolveLauncherExecutablePath();
            ProcessStartInfo startInfo = new ProcessStartInfo(launcherPath)
            {
                WorkingDirectory = Path.GetDirectoryName(launcherPath),
                UseShellExecute = false
            };

            try
            {
                App.ReleaseSingleInstanceMutex();
                Process restartedProcess = Process.Start(startInfo);
                if (restartedProcess != null)
                {
                    System.Threading.Thread.Sleep(750);
                    if (!restartedProcess.HasExited)
                    {
                        Environment.Exit(0);
                    }

                    App.RecreateSingleInstanceMutex();
                    this._relaunchQueued = false;
                    this.Status(text6);
                    this.AddLogEntry("Redémarrage du launcher impossible : le nouveau processus s'est fermé immédiatement.");
                    this.ShowLogsTab();
                    return;
                }

                App.RecreateSingleInstanceMutex();
                this._relaunchQueued = false;
                this.Status(text6);
                this.AddLogEntry("Redémarrage du launcher impossible : aucun processus n'a été créé.");
                this.ShowLogsTab();
            }
            catch (Exception ex)
            {
                App.RecreateSingleInstanceMutex();
                this._relaunchQueued = false;
                Log.PublishWarning(text3 + " => " + ex.Message);
                this.Status(text6);
                this.AddLogEntry("Redémarrage du launcher impossible : " + ex.Message);
                this.ShowLogsTab();
            }
        }

        private string ResolveLauncherExecutablePath()
        {
            if (!string.IsNullOrWhiteSpace(this.CurrentLauncherPath) && File.Exists(this.CurrentLauncherPath))
            {
                return this.CurrentLauncherPath;
            }

            string baseDirectoryCandidate = Path.Combine(AppContext.BaseDirectory, this.CurrentLauncherFilename + ".exe");
            if (File.Exists(baseDirectoryCandidate))
            {
                this.CurrentLauncherPath = baseDirectoryCandidate;
                return baseDirectoryCandidate;
            }

            string currentDirectoryCandidate = Path.Combine(Directory.GetCurrentDirectory(), this.CurrentLauncherFilename + ".exe");
            if (File.Exists(currentDirectoryCandidate))
            {
                this.CurrentLauncherPath = currentDirectoryCandidate;
                return currentDirectoryCandidate;
            }

            return baseDirectoryCandidate;
        }

        private void QueueRelaunchOnce()
        {
            if (this._relaunchQueued)
            {
                return;
            }

            this._relaunchQueued = true;
            this.Commands.Enqueue(CommandQueue.Relaunch);
        }

        private bool ShouldRelaunchAfterUpdatingFile(string currentFile)
        {
            if (string.IsNullOrWhiteSpace(currentFile))
            {
                return false;
            }

            string fileName = Path.GetFileName(currentFile);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            return string.Equals(fileNameWithoutExtension, this.CurrentLauncherFilename, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileName, "configuration.xml", StringComparison.OrdinalIgnoreCase);
        }

        private void InitializeNotifyIconMenu()
        {
            System.Windows.Forms.ContextMenuStrip contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            this._trayOpenMenuItem = new System.Windows.Forms.ToolStripMenuItem("Ouvrir");
            this._trayLaunchMenuItem = new System.Windows.Forms.ToolStripMenuItem("Lancer Dofus");
            this._trayQuitMenuItem = new System.Windows.Forms.ToolStripMenuItem("Quitter");

            this._trayOpenMenuItem.Click += (sender, args) => this.RestoreFromTray();
            this._trayLaunchMenuItem.Click += (sender, args) => this.LaunchDofus(true);
            this._trayQuitMenuItem.Click += (sender, args) => Application.Current.Shutdown();

            contextMenuStrip.Items.Add(this._trayOpenMenuItem);
            contextMenuStrip.Items.Add(this._trayLaunchMenuItem);
            contextMenuStrip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenuStrip.Items.Add(this._trayQuitMenuItem);
            contextMenuStrip.Opening += (sender, args) => this.RefreshTrayMenuState();

            MyNotifyIcon.ContextMenuStrip = contextMenuStrip;
            this.RefreshTrayMenuState();
        }

        private void RefreshTrayMenuState()
        {
            if (this._trayLaunchMenuItem != null)
            {
                this._trayLaunchMenuItem.Enabled = this.CanUsePlayButton();
            }
        }

        private void ShowVersionCheckButton(bool isEnabled = true)
        {
            this.VersionCHKButton.Visibility = Visibility.Visible;
            this.VersionCHKButton.IsHitTestVisible = isEnabled;
            this.VersionCHKButton.Focusable = isEnabled;
        }

        private void HideUpdateStatusDisplay()
        {
            this.lblUpdatePercentage.Visibility = Visibility.Hidden;
            this.textState.Visibility = Visibility.Hidden;
            this.progrees_bar.Visibility = Visibility.Hidden;
        }

        private void LogStatusOnly(string status)
        {
            if (Configuration.LoadBooleanEntry("StatusToLog", false))
            {
                Log.PublishLog(status);
            }

            this.AddLogEntry(status);
            this.HideUpdateStatusDisplay();
        }

        private void RestoreFromTray()
        {
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
            if (MyNotifyIcon != null)
            {
                MyNotifyIcon.Visible = false;
            }
        }

        private void MinimizeToTray(bool showBalloonTip)
        {
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
            if (MyNotifyIcon == null)
            {
                return;
            }

            MyNotifyIcon.Visible = true;
            if (showBalloonTip)
            {
                MyNotifyIcon.BalloonTipTitle = text14;
                MyNotifyIcon.BalloonTipText = text15;
                MyNotifyIcon.ShowBalloonTip(400);
            }
        }

        private bool LaunchDofus(bool minimizeAfterLaunch)
        {
            if (!this.CanUsePlayButton())
            {
                this.RefreshPlayButtonDisabledState();
                return false;
            }

            try
            {
                this.savelang();
                ArkanSound.Initialization.Init();

                if (File.Exists(@"reg\Reg.exe"))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.GetFullPath(@"reg\Reg.exe"),
                        WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(@"reg\Reg.exe")),
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    System.Threading.Thread.Sleep(800);
                }

                Process gameProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = Path.GetFullPath("Dofus.exe"),
                    WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath("Dofus.exe")),
                    UseShellExecute = true
                });

                if (gameProcess == null)
                {
                    throw new InvalidOperationException("Dofus.exe n'a pas pu être lancé.");
                }

                if (minimizeAfterLaunch)
                {
                    this.MinimizeToTray(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                this.RestoreFromTray();
                this.AddLogEntry(this.GetLocalizedLaunchFailedPrefix() + ex.Message);
                this.ShowLogsTab();
                return false;
            }
        }

        public void Status(string status, int porcentage)
        {
            if (Configuration.LoadBooleanEntry("StatusToLog", false))
            {
                Log.PublishLog(status);
            }

            this.textState.Content = status;
            this.AddLogEntry(status);
            this.lblUpdatePercentage.Content = porcentage.ToString() + text4;
            this.lblUpdatePercentage.Visibility = Visibility.Visible;
            this.playButton.Visibility = Visibility.Visible;
            this.RefreshPlayButtonIdleState();
            this.RefreshVersionCheckButtonWaitingState();
            this.progrees_bar.Visibility = Visibility.Visible;
            this.textState.Visibility = Visibility.Visible;
            this.progrees_bar.Value = Math.Max(Math.Min(porcentage, (int)this.progrees_bar.Maximum), (int)this.progrees_bar.Minimum);
        }

        public void Status(string status)
        {
            if (Configuration.LoadBooleanEntry("StatusToLog", false))
            {
                Log.PublishLog(status);
            }

            this.textState.Content = status;
            this.AddLogEntry(status);
        }

        public void StatusDetails(int filesLeft, int bytesTotal, int bytesLoaded)
        {
            float mbtotal = (float)bytesTotal / 1024f / 1024f;
            float mbloaded = (float)bytesLoaded / 1024f / 1024f;
            string details = string.Format(text5, filesLeft) + " - " + string.Format(text7, mbloaded, mbtotal);
            this.textState.Content = details;
        }

        private void ResetProgress()
        {
            this._launcherReady = true;
            this._lastDownloadLogMilestone = -1;
            this.progrees_bar.Value = this.progrees_bar.Maximum;
            this.textState.Content = "";
            this.playButton.Visibility = Visibility.Visible;
            this.HideUpdateStatusDisplay();
            this.RefreshPlayButtonIdleState();
            this.RefreshVersionCheckButtonIdleState();
        }

        private void FinishProgress()
        {
            this._launcherReady = true;
            this._lastDownloadLogMilestone = -1;
            this.progrees_bar.Value = this.progrees_bar.Maximum;
            this.textState.Content = "";
            this.playButton.Visibility = Visibility.Visible;
            this.HideUpdateStatusDisplay();
            this.RefreshPlayButtonIdleState();
            this.RefreshVersionCheckButtonIdleState();
        }

        private void UpdateWorkerInstance_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateWorker.UpdateWorkerProgressState UpdaterProgressState = (UpdateWorker.UpdateWorkerProgressState)e.UserState;
            switch (UpdaterProgressState.State)
            {
                case UpdateWorker.UpdateWorkerState.Starting:
                    this.progrees_bar.Value = this.progrees_bar.Minimum;
                    this.LogStatusOnly(text8);
                    break;

                case UpdateWorker.UpdateWorkerState.FetchingVer:
                    this.LogStatusOnly(text9);
                    break;

                case UpdateWorker.UpdateWorkerState.FetchingVerInfo:
                    this.LogStatusOnly(text10);
                    break;

                case UpdateWorker.UpdateWorkerState.CheckingDiff:
                    this.LogStatusOnly(string.Format(text11, UpdaterProgressState.ServerFileCount, UpdaterProgressState.LocalFileCount));
                    break;

                case UpdateWorker.UpdateWorkerState.UpdatingDiff:
                    this.playButton.Visibility = Visibility.Visible;
                    this.RefreshPlayButtonIdleState();
                    this.RefreshVersionCheckButtonWaitingState();
                    this.lblUpdatePercentage.Visibility = Visibility.Visible;
                    this.progrees_bar.Visibility = Visibility.Visible;
                    this.textState.Visibility = Visibility.Visible;
                    this.progrees_bar.Value = Math.Max(Math.Min(e.ProgressPercentage, (int)this.progrees_bar.Maximum), (int)this.progrees_bar.Minimum);
                    this.lblUpdatePercentage.Content = e.ProgressPercentage.ToString() + text4;
                    this.textState.Content = string.Format(text12, e.ProgressPercentage);
                    if (this.ShouldRelaunchAfterUpdatingFile(UpdaterProgressState.CurrentFile))
                    {
                        this.QueueRelaunchOnce();
                    }
                    this.LogDownloadProgressIfNeeded(e.ProgressPercentage);
                    break;

                case UpdateWorker.UpdateWorkerState.Finished:
                    Dispatcher.Invoke((Delegate)new UpdateProgressDelegate(this.UpdatePercentage), (object)100);
                    textState.Content = text6;
                    progrees_bar.Visibility = Visibility.Hidden;
                    textState.Visibility = Visibility.Hidden;
                    playButton.Visibility = Visibility.Visible;
                    this.FinishProgress();
                    this.Status(text6);
                    break;

                case UpdateWorker.UpdateWorkerState.CheckingFiles:
                    this.LogStatusOnly(text17);
                    break;

                case UpdateWorker.UpdateWorkerState.CheckingFilesError:
                    this.Commands.Clear();
                    this._relaunchQueued = false;
                    this.Status(text18);
                    this.FinishProgress();
                    if (UpdaterProgressState.CurrentFile == null)
                    {
                        this.AddLogEntry(text19);
                    }
                    else
                    {
                        this.AddLogEntry(string.Format(text21, UpdaterProgressState.CurrentFile));
                    }
                    this.ShowLogsTab();
                    break;

                case UpdateWorker.UpdateWorkerState.VerinfoFail:
                    this.Commands.Clear();
                    this._relaunchQueued = false;
                    this.FinishProgress();
                    this.Status(text22);
                    this.AddLogEntry(text23);
                    this.ShowLogsTab();
                    break;
            }
        }

        private void UpdatePercentage(int percentage)
        {
            try
            {
                progrees_bar.Value = percentage;
            }
            catch
            {
                textState.Content = text1;
                progrees_bar.Foreground = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/UI/progress bar/progress_bar_rouge.png")));
                return;
            }
        }

        private void LoadConfig()
        {
            if (!Directory.Exists("data/Launcher"))
            {
                Directory.CreateDirectory("data/Launcher");
            }

            if (File.Exists("data/Launcher/lang.txt"))
            {
                using (StreamReader streamReader = new StreamReader("data/Launcher/lang.txt"))
                {
                    this.lang = streamReader.ReadLine();
                }
            }
            else
            {
                this.lang = "fr";
                using (StreamWriter streamWriter = new StreamWriter("data/Launcher/lang.txt"))
                {
                    streamWriter.WriteLine("fr");
                }
            }

            Console.WriteLine("Lingua:" + lang);

            if (File.Exists("data/Launcher/cor.txt"))
            {
                using (StreamReader streamReader = new StreamReader("data/Launcher/cor.txt"))
                {
                    this.cor = bool.Parse(streamReader.ReadLine());
                }
            }
            else
            {
                this.cor = false;
                using (StreamWriter streamWriter = new StreamWriter("data/Launcher/cor.txt"))
                {
                    streamWriter.WriteLine(cor);
                }
            }

            Console.WriteLine("Cor:" + cor);
        }

        private string GetNormalizedLanguageCode()
        {
            string code = (this.lang ?? "fr").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                return "FR";
            }

            switch (code)
            {
                case "JA":
                    return "JP";
                case "UK":
                case "GB":
                    return "EN";
                default:
                    return code;
            }
        }

        private string GetMenuButtonLanguageCode()
        {
            string code = this.GetNormalizedLanguageCode();
            switch (code)
            {
                case "BR":
                    return "pt";
                case "EN":
                case "US":
                case "DE":
                case "IT":
                case "JP":
                case "NL":
                case "RU":
                    return "en";
                case "ES":
                    return "es";
                case "PT":
                    return "pt";
                default:
                    return "fr";
            }
        }

        private IReadOnlyList<string> GetRssLanguageCandidates()
        {
            string code = this.GetNormalizedLanguageCode();
            switch (code)
            {
                case "JP":
                    return new[] { "JP", "JA" };
                case "US":
                    return new[] { "US", "EN" };
                case "BR":
                    return new[] { "BR", "PT" };
                default:
                    return new[] { code };
            }
        }

        private string GetFlagImageUri(string code, bool hoveredOrSelected)
        {
            string suffix = hoveredOrSelected ? "_click" : string.Empty;
            return "pack://application:,,,/UI/Flags/" + code.ToUpperInvariant() + suffix + ".png";
        }

        private string GetPlayButtonImageUri(int state)
        {
            string code = this.GetNormalizedLanguageCode();
            if (!this._playButtonLanguages.Contains(code))
            {
                return "pack://application:,,,/UI/Play/1_OFF.png";
            }

            switch (code)
            {
                case "BR":
                case "JA":
                case "JP":
                case "NL":
                case "US":
                    code = "EN";
                    break;
            }

            return "pack://application:,,,/UI/Play/" + code + "_" + state + ".png";
        }

        private void SetImage(Button button, string uri)
        {
            button.Background = new ImageBrush()
            {
                ImageSource = new BitmapImage(new Uri(uri)),
                Stretch = Stretch.Fill
            };
        }

        private void InitializeLanguageMenu()
        {
            this.langList.ItemsSource = this._flagCodes
                .Select(code => new FlagItem
                {
                    Code = code,
                    NormalImage = this.GetFlagImageUri(code, false),
                    HoverImage = this.GetFlagImageUri(code, true)
                })
                .ToList();
        }

        private void UpdateSelectedLanguageButton()
        {
            this.SetImage(this.langButton, this.GetFlagImageUri(this.GetNormalizedLanguageCode(), true));
        }

        private void RefreshLanguageSelection()
        {
            FlagItem selectedFlag = this.langList.Items.Cast<FlagItem>()
                .FirstOrDefault(item => item.Code.Equals(this.GetNormalizedLanguageCode(), StringComparison.OrdinalIgnoreCase));
            this._isUpdatingLanguageSelection = true;
            this.langList.SelectedItem = selectedFlag;
            this.UpdateSelectedLanguageButton();
            this._isUpdatingLanguageSelection = false;
        }

        private void ShowLanguageMenu(bool visible)
        {
            this.langList.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            Panel.SetZIndex(this.langList, visible ? 999 : 0);
        }

        private void UpdateNewsAndLogsTabLabels()
        {
            string code = this.GetNormalizedLanguageCode();
            switch (code)
            {
                case "DE":
                    this.NewsTabTextBlock.Text = "News";
                    this.LogsTabTextBlock.Text = "Logs";
                    break;
                case "EN":
                case "US":
                    this.NewsTabTextBlock.Text = "News";
                    this.LogsTabTextBlock.Text = "Logs";
                    break;
                case "ES":
                    this.NewsTabTextBlock.Text = "Noticias";
                    this.LogsTabTextBlock.Text = "Logs";
                    break;
                case "IT":
                    this.NewsTabTextBlock.Text = "News";
                    this.LogsTabTextBlock.Text = "Log";
                    break;
                case "BR":
                case "PT":
                    this.NewsTabTextBlock.Text = "Notícias";
                    this.LogsTabTextBlock.Text = "Logs";
                    break;
                case "JA":
                case "JP":
                    this.NewsTabTextBlock.Text = "ニュース";
                    this.LogsTabTextBlock.Text = "ログ";
                    break;
                case "NL":
                    this.NewsTabTextBlock.Text = "Nieuws";
                    this.LogsTabTextBlock.Text = "Logs";
                    break;
                case "RU":
                    this.NewsTabTextBlock.Text = "Новости";
                    this.LogsTabTextBlock.Text = "Логи";
                    break;
                default:
                    this.NewsTabTextBlock.Text = "News";
                    this.LogsTabTextBlock.Text = "Logs";
                    break;
            }
        }

        private void RefreshWindowFrameButtonsIdleState()
        {
            this.SetImage(this.closeButton, "pack://application:,,,/UI/Frames/Close/1.png");
            this.SetImage(this.reduceButton, "pack://application:,,,/UI/Frames/Mini/1.png");
        }

        private bool CanUsePlayButton()
        {
            return this._launcherReady && this._playButtonLanguages.Contains(this.GetNormalizedLanguageCode());
        }

        private void RefreshPlayButtonDisabledState()
        {
            this.SetImage(this.playButton, "pack://application:,,,/UI/Play/1_OFF.png");
            this.playButton.IsHitTestVisible = false;
            this.playButton.Focusable = false;
            this.RefreshTrayMenuState();
        }

        private void RefreshPlayButtonIdleState()
        {
            if (!this.CanUsePlayButton())
            {
                this.RefreshPlayButtonDisabledState();
                return;
            }

            this.playButton.IsHitTestVisible = true;
            this.playButton.Focusable = true;
            this.SetImage(this.playButton, this.GetPlayButtonImageUri(1));
            this.RefreshTrayMenuState();
        }

        private void RefreshPlayButtonHoverState()
        {
            if (!this.CanUsePlayButton())
            {
                this.RefreshPlayButtonDisabledState();
                return;
            }

            this.playButton.IsHitTestVisible = true;
            this.playButton.Focusable = true;
            this.SetImage(this.playButton, this.GetPlayButtonImageUri(2));
            this.RefreshTrayMenuState();
        }

        private void RefreshPlayButtonPressedState()
        {
            if (!this.CanUsePlayButton())
            {
                this.RefreshPlayButtonDisabledState();
                return;
            }

            this.playButton.IsHitTestVisible = true;
            this.playButton.Focusable = true;
            this.SetImage(this.playButton, this.GetPlayButtonImageUri(3));
            this.RefreshTrayMenuState();
        }

        private void RefreshVersionCheckButtonIdleState()
        {
            this.ShowVersionCheckButton();
            this.SetImage(this.VersionCHKButton, "pack://application:,,,/UI/Frames/Set/1.png");
        }

        private void RefreshVersionCheckButtonWaitingState()
        {
            this.ShowVersionCheckButton(false);
            this.SetImage(this.VersionCHKButton, "pack://application:,,,/UI/Frames/Set/2.png");
        }

        private void RefreshVersionCheckButtonHoverState()
        {
            this.ShowVersionCheckButton();
            this.SetImage(this.VersionCHKButton, "pack://application:,,,/UI/Frames/Set/3.png");
        }

        private void RefreshVersionCheckButtonPressedState()
        {
            this.ShowVersionCheckButton();
            this.SetImage(this.VersionCHKButton, "pack://application:,,,/UI/Frames/Set/4.png");
        }

        private void InitializeNewsAndLogs()
        {
            this.LoadNewsFeed();
            this.AddLogEntry(this.GetLocalizedLauncherReadyMessage());
        }

        private void ShowNewsTab()
        {
            this.newsList.Visibility = Visibility.Visible;
            this.logsList.Visibility = Visibility.Collapsed;
            this.newsTabImage.Source = new BitmapImage(new Uri("pack://application:,,,/Uplauncher;component/UI/Frames/Window/1.png"));
            this.logsTabImage.Source = new BitmapImage(new Uri("pack://application:,,,/Uplauncher;component/UI/Frames/Window/2.png"));
            this.NewsTabTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#FF2F2418");
            this.LogsTabTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#FF2F2418");
            Panel.SetZIndex(this.newsTabButton, 2);
            Panel.SetZIndex(this.logsTabButton, 1);
        }

        private void ShowLogsTab()
        {
            this.newsList.Visibility = Visibility.Collapsed;
            this.logsList.Visibility = Visibility.Visible;
            this.newsTabImage.Source = new BitmapImage(new Uri("pack://application:,,,/Uplauncher;component/UI/Frames/Window/2.png"));
            this.logsTabImage.Source = new BitmapImage(new Uri("pack://application:,,,/Uplauncher;component/UI/Frames/Window/1.png"));
            this.NewsTabTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#FF2F2418");
            this.LogsTabTextBlock.Foreground = (Brush)new BrushConverter().ConvertFrom("#FF2F2418");
            Panel.SetZIndex(this.newsTabButton, 1);
            Panel.SetZIndex(this.logsTabButton, 2);
        }

        private void LogDownloadProgressIfNeeded(int percentage)
        {
            int clampedPercentage = Math.Max(0, Math.Min(percentage, 100));
            int milestone = (clampedPercentage / 25) * 25;
            if (milestone <= 0 || milestone >= 100)
            {
                return;
            }

            if (milestone == this._lastDownloadLogMilestone)
            {
                return;
            }

            this._lastDownloadLogMilestone = milestone;
            this.AddLogEntry(string.Format(text12, milestone));
        }

        private void AddLogEntry(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            message = message.Trim();
            if (string.Equals(this._lastLogMessage, message, StringComparison.Ordinal))
            {
                return;
            }

            this._lastLogMessage = message;
            this.logsList.Items.Add(new LogItem { Message = message });
            this.logsList.ScrollIntoView(this.logsList.Items[this.logsList.Items.Count - 1]);
        }

        private string GetNewsLogoUri(string text)
        {
            string value = (text ?? string.Empty).ToLowerInvariant();
            if (value.Contains("ankama"))
            {
                return "pack://application:,,,/UI/Frames/Mini Logo/Ankama.png";
            }

            return "pack://application:,,,/UI/Frames/Mini Logo/Dofus.png";
        }

        private void LoadNewsFeed()
        {
            this.newsList.Items.Clear();

            bool loaded = this.TryLoadRssFeed(DofusRssUrl, "Dofus");

            if (!loaded)
            {
                this.newsList.Items.Add(new NewsItem
                {
                    Title = this.GetLocalizedNewsFeedUnavailableTitle(),
                    Summary = this.GetLocalizedNewsFeedUnavailableSummary(),
                    DateText = this.GetLocalizedNewsFeedUnavailableDateText(),
                    Link = DofusRssUrl,
                    LogoImage = this.GetNewsLogoUri("Ankama")
                });
            }
        }

        private bool TryLoadRssFeed(string url, string sourceName)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                };

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36 SunshineUplauncher/1.0");
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/xml, text/xml;q=0.9, */*;q=0.8");
                    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7");
                    client.DefaultRequestHeaders.Referrer = new Uri("https://retro-dofus.com/");

                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
                        request.Headers.TryAddWithoutValidation("Pragma", "no-cache");

                        using (HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult())
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                return false;
                            }

                            string xml = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            return this.TryLoadRssXml(xml, sourceName);
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetElementValue(XElement parent, string localName)
        {
            if (parent == null)
            {
                return string.Empty;
            }

            XElement element = parent.Elements().FirstOrDefault(x =>
                string.Equals(x.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));

            return element != null ? (element.Value ?? string.Empty) : string.Empty;
        }

        private bool TryLoadRssXml(string xml, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return false;
            }

            try
            {
                XDocument document = XDocument.Parse(xml);
                IReadOnlyList<string> languageCandidates = this.GetRssLanguageCandidates();

                XElement channel = document.Descendants()
                    .FirstOrDefault(x => string.Equals(x.Name.LocalName, "channel", StringComparison.OrdinalIgnoreCase));

                string channelLogo = GetElementValue(channel, "logo").Trim();

                var allItems = document.Descendants()
                    .Where(x => string.Equals(x.Name.LocalName, "item", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (allItems.Count == 0)
                {
                    return false;
                }

                var items = allItems
                    .Where(x => languageCandidates.Contains(GetElementValue(x, "language").Trim(), StringComparer.OrdinalIgnoreCase))
                    .Take(8)
                    .ToList();

                if (items.Count == 0)
                {
                    items = allItems
                        .Where(x => string.Equals(GetElementValue(x, "language").Trim(), "EN", StringComparison.OrdinalIgnoreCase))
                        .Take(8)
                        .ToList();
                }

                if (items.Count == 0)
                {
                    items = allItems
                        .Where(x => string.IsNullOrWhiteSpace(GetElementValue(x, "language").Trim()))
                        .Take(8)
                        .ToList();
                }

                if (items.Count == 0)
                {
                    items = allItems.Take(8).ToList();
                }

                foreach (var item in items)
                {
                    string title = GetElementValue(item, "title").Trim();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        title = this.GetLocalizedGenericNewsTitle(sourceName);
                    }

                    string description = GetElementValue(item, "description").Trim();
                    string link = GetElementValue(item, "link").Trim();
                    string category = GetElementValue(item, "category").Trim();
                    string pubDate = GetElementValue(item, "pubDate").Trim();
                    string itemLogo = GetElementValue(item, "logo").Trim();

                    string logoKey = !string.IsNullOrWhiteSpace(itemLogo)
                        ? itemLogo
                        : (!string.IsNullOrWhiteSpace(channelLogo) ? channelLogo : sourceName + " " + title);

                    string localizedTitle = this.LocalizeNewsTitle(title);
                    string localizedDescription = this.LocalizeNewsDescription(description);
                    string dateText = this.GetLocalizedNewsDateText(pubDate, category);

                    this.newsList.Items.Add(new NewsItem
                    {
                        Title = localizedTitle,
                        Summary = localizedDescription,
                        DateText = dateText,
                        Link = link,
                        LogoImage = this.GetNewsLogoUri(logoKey)
                    });
                }

                return this.newsList.Items.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private string GetLocalizedLauncherReadyMessage()
        {
            switch (this.GetNormalizedLanguageCode())
            {
                case "DE": return "Launcher bereit.";
                case "EN":
                case "US": return "Launcher ready.";
                case "ES": return "Launcher listo.";
                case "IT": return "Launcher pronto.";
                case "BR":
                case "PT": return "Launcher pronto.";
                case "JP": return "ランチャーの準備が完了しました。";
                case "NL": return "Launcher gereed.";
                case "RU": return "Лаунчер готов.";
                default: return "Launcher prêt.";
            }
        }

        private string GetLocalizedLaunchFailedPrefix()
        {
            switch (this.GetNormalizedLanguageCode())
            {
                case "DE": return "Start fehlgeschlagen: ";
                case "EN":
                case "US": return "Launch failed: ";
                case "ES": return "Error de inicio: ";
                case "IT": return "Avvio non riuscito: ";
                case "BR":
                case "PT": return "Falha ao iniciar: ";
                case "JP": return "起動に失敗しました: ";
                case "NL": return "Start mislukt: ";
                case "RU": return "Ошибка запуска: ";
                default: return "Échec du lancement : ";
            }
        }

        private string GetLocalizedNewsFeedUnavailableTitle()
        {
            switch (this.GetNormalizedLanguageCode())
            {
                case "DE": return "RSS-Feed nicht verfügbar";
                case "EN":
                case "US": return "RSS feed unavailable";
                case "ES": return "Flujo RSS no disponible";
                case "IT": return "Feed RSS non disponibile";
                case "BR":
                case "PT": return "Feed RSS indisponível";
                case "JP": return "RSSフィードを利用できません";
                case "NL": return "RSS-feed niet beschikbaar";
                case "RU": return "RSS-лента недоступна";
                default: return "Flux RSS indisponible";
            }
        }

        private string GetLocalizedNewsFeedUnavailableSummary()
        {
            switch (this.GetNormalizedLanguageCode())
            {
                case "DE": return "Lege deine rss.xml online ab, um die News anzuzeigen.";
                case "EN":
                case "US": return "Put your rss.xml online to display the news.";
                case "ES": return "Sube tu rss.xml en línea para mostrar las noticias.";
                case "IT": return "Metti il tuo rss.xml online per mostrare le notizie.";
                case "BR":
                case "PT": return "Coloca o teu rss.xml online para mostrar as notícias.";
                case "JP": return "ニュースを表示するには rss.xml をオンラインに配置してください。";
                case "NL": return "Zet je rss.xml online om het nieuws te tonen.";
                case "RU": return "Разместите rss.xml в сети, чтобы показывать новости.";
                default: return "Ajoute ton rss.xml en ligne pour afficher les news.";
            }
        }

        private string GetLocalizedNewsFeedUnavailableDateText()
        {
            switch (this.GetNormalizedLanguageCode())
            {
                case "DE": return "Warte auf News-Feed";
                case "EN":
                case "US": return "Waiting for the news feed";
                case "ES": return "Esperando el flujo de noticias";
                case "IT": return "In attesa del feed notizie";
                case "BR":
                case "PT": return "À espera do feed de notícias";
                case "JP": return "ニュースフィードを待機中";
                case "NL": return "Wachten op de nieuwsfeed";
                case "RU": return "Ожидание ленты новостей";
                default: return "En attente du flux de news";
            }
        }

        private string GetLocalizedGenericNewsTitle(string sourceName)
        {
            string prefix = string.IsNullOrWhiteSpace(sourceName) ? "Launcher" : sourceName.Trim();
            switch (this.GetNormalizedLanguageCode())
            {
                case "DE": return prefix + " News";
                case "EN":
                case "US": return prefix + " news";
                case "ES": return "Noticias de " + prefix;
                case "IT": return "Notizie di " + prefix;
                case "BR":
                case "PT": return "Notícias de " + prefix;
                case "JP": return prefix + " ニュース";
                case "NL": return prefix + " nieuws";
                case "RU": return "Новости " + prefix;
                default: return "News " + prefix;
            }
        }

        private string LocalizeNewsTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            string normalizedTitle = title.Trim();
            if (!string.Equals(normalizedTitle, "Mise à jour du launcher", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedTitle;
            }

            switch (this.GetNormalizedLanguageCode())
            {
                case "DE": return "Launcher-Update";
                case "EN":
                case "US": return "Launcher update";
                case "ES": return "Actualización del launcher";
                case "IT": return "Aggiornamento del launcher";
                case "BR":
                case "PT": return "Atualização do launcher";
                case "JP": return "ランチャーのアップデート";
                case "NL": return "Launcher-update";
                case "RU": return "Обновление лаунчера";
                default: return normalizedTitle;
            }
        }

        private string LocalizeNewsDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return string.Empty;
            }

            string normalizedDescription = description.Trim();
            if (!string.Equals(normalizedDescription, "Corrections et améliorations du launcher", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedDescription;
            }

            switch (this.GetNormalizedLanguageCode())
            {
                case "DE": return "Korrekturen und Verbesserungen des Launchers";
                case "EN":
                case "US": return "Launcher fixes and improvements";
                case "ES": return "Correcciones y mejoras del launcher";
                case "IT": return "Correzioni e miglioramenti del launcher";
                case "BR":
                case "PT": return "Correções e melhorias do launcher";
                case "JP": return "ランチャーの修正と改善";
                case "NL": return "Launcher-correcties en verbeteringen";
                case "RU": return "Исправления и улучшения лаунчера";
                default: return normalizedDescription;
            }
        }

        private string GetLocalizedNewsDateText(string pubDate, string category)
        {
            string prefixCategory = string.IsNullOrWhiteSpace(category) ? string.Empty : category.Trim() + " • ";
            DateTime parsedDate;
            if (DateTime.TryParse(pubDate, out parsedDate))
            {
                switch (this.GetNormalizedLanguageCode())
                {
                    case "DE": return prefixCategory + "Veröffentlicht am " + parsedDate.ToString("dd/MM/yyyy");
                    case "EN":
                    case "US": return prefixCategory + "Posted on " + parsedDate.ToString("dd/MM/yyyy");
                    case "ES": return prefixCategory + "Publicado el " + parsedDate.ToString("dd/MM/yyyy");
                    case "IT": return prefixCategory + "Pubblicato il " + parsedDate.ToString("dd/MM/yyyy");
                    case "BR":
                    case "PT": return prefixCategory + "Publicado em " + parsedDate.ToString("dd/MM/yyyy");
                    case "JP": return prefixCategory + parsedDate.ToString("yyyy/MM/dd") + " に投稿";
                    case "NL": return prefixCategory + "Geplaatst op " + parsedDate.ToString("dd/MM/yyyy");
                    case "RU": return prefixCategory + "Опубликовано " + parsedDate.ToString("dd/MM/yyyy");
                    default: return prefixCategory + "Posté le " + parsedDate.ToString("dd/MM/yyyy");
                }
            }

            return prefixCategory + pubDate;
        }

        private void savecor()
        {
            StreamWriter streamWriter = new StreamWriter("data/Launcher/cor.txt");
            streamWriter.WriteLine(cor);
            streamWriter.Close();
        }

        private void checkcor()
        {
            string menuLang = this.GetMenuButtonLanguageCode();
            if (cor == false)
            {
                textState.Foreground = Brushes.Black;
                this.window.Source = new BitmapImage(new Uri("pack://application:,,,/UI/win.png"));
                this.progrees_bar.Background = new ImageBrush()
                {
                    ImageSource = new BitmapImage(new Uri("pack://application:,,,/UI/Download/DL_0.png")),
                };
                this.langList.Background = (Brush)new BrushConverter().ConvertFrom("#FFFEFEFE");
                cor = true;
            }
            else
            {
                textState.Foreground = Brushes.White;
                this.window.Source = new BitmapImage(new Uri("pack://application:,,,/UI/win.png"));
                this.progrees_bar.Background = new ImageBrush()
                {
                    ImageSource = new BitmapImage(new Uri("pack://application:,,,/UI/Download/DL_0.png")),
                };
                this.langList.Background = (Brush)new BrushConverter().ConvertFrom("#FF242424");
                cor = false;
            }

            this.RefreshWindowFrameButtonsIdleState();
            this.RefreshPlayButtonIdleState();
            this.RefreshVersionCheckButtonIdleState();
            this.RefreshLanguageSelection();
        }

        private void checklang()
        {
            this.RefreshLanguageSelection();
            switch (this.GetNormalizedLanguageCode())
            {
                case "DE":
                    text1 = "Fehler beim Aktualisieren der Oberfläche.";
                    text3 = "Der Launcher konnte nicht neu gestartet werden!";
                    text4 = "%";
                    text5 = "{0} Dateien zu verarbeiten";
                    text6 = "Download der Updates abgeschlossen.";
                    text7 = "Dateien werden heruntergeladen ...";
                    text8 = "Launcher-Update läuft ...";
                    text9 = "Version wird abgerufen ...";
                    text10 = "Versionskonflikt. Versionsinformationen werden abgerufen ...";
                    text11 = "Dateiunterschied wird geprüft. Server: {0} Datei(en) Lokal: {1} Datei(en)";
                    text12 = "Spiel wird aktualisiert...";
                    text13 = "Installierte Version wird gelesen.";
                    text14 = "Sunshine minimiert";
                    text15 = "Sunshine befindet sich jetzt in der Taskleiste";
                    text17 = "Dateimodus wird geprüft.";
                    text18 = "Dateiprüfung fehlgeschlagen.";
                    text19 = "Unbekannte Datei wird verwendet. Bitte alle Clients schließen und das Update erneut starten!";
                    text21 = "Datei {0} wird verwendet. Bitte Client schließen und das Update erneut starten!";
                    text22 = "Dateiprüfung fehlgeschlagen!";
                    text23 = "Die neueste Spielversion konnte nicht abgerufen werden!";
                    text24 = "Die Versionsprüfung startet, sobald der aktuelle Vorgang beendet ist";
                    break;
                case "EN":
                case "US":
                    text1 = "Failed to refresh the interface.";
                    text3 = "Unable to restart the launcher!";
                    text4 = "%";
                    text5 = "{0} files to process";
                    text6 = "Update download completed.";
                    text7 = "Downloading files ...";
                    text8 = "Updating launcher startup ...";
                    text9 = "Fetching version ...";
                    text10 = "Version mismatch. Fetching version information ...";
                    text11 = "Checking file differences. Server: {0} file(s) Local: {1} file(s)";
                    text12 = "Updating the game...";
                    text13 = "Reading the currently installed version.";
                    text14 = "Sunshine minimized";
                    text15 = "Sunshine is now in the taskbar";
                    text17 = "Checking file mode.";
                    text18 = "File check failed.";
                    text19 = "An unknown file is busy. Close all clients and run the update again please!";
                    text21 = "File {0} is busy. Close the client and run the updater again!";
                    text22 = "File verification failed!";
                    text23 = "Failed to retrieve the latest game version!";
                    text24 = "Version check starts after the active operation finishes";
                    break;
                case "ES":
                    text1 = "Error al actualizar la interfaz.";
                    text3 = "¡No se puede reiniciar el launcher!";
                    text4 = "%";
                    text5 = "{0} archivos por procesar";
                    text6 = "Descarga de actualizaciones completada.";
                    text7 = "Descargando archivos ...";
                    text8 = "Actualizando el inicio del launcher ...";
                    text9 = "Recuperando la versión ...";
                    text10 = "Incompatibilidad de versión. Recuperando la información de versión ...";
                    text11 = "Comprobando diferencias de archivos. Servidor: {0} archivo(s) Local: {1} archivo(s)";
                    text12 = "Actualizando el juego...";
                    text13 = "Leyendo la versión instalada actualmente.";
                    text14 = "Sunshine minimizado";
                    text15 = "Sunshine ahora está en la barra de tareas";
                    text17 = "Comprobando el modo de archivos.";
                    text18 = "La verificación de archivos falló.";
                    text19 = "Un archivo desconocido está ocupado. ¡Cierra todos los clientes y vuelve a lanzar la actualización!";
                    text21 = "El archivo {0} está ocupado. ¡Cierra el cliente y vuelve a lanzar la actualización!";
                    text22 = "¡La verificación de archivos falló!";
                    text23 = "¡No se pudo obtener la última versión del juego!";
                    text24 = "La verificación de versión comenzará cuando termine la operación activa";
                    break;
                case "IT":
                    text1 = "Errore durante l'aggiornamento dell'interfaccia.";
                    text3 = "Impossibile riavviare il launcher!";
                    text4 = "%";
                    text5 = "{0} file da elaborare";
                    text6 = "Download aggiornamenti completato.";
                    text7 = "Download dei file in corso ...";
                    text8 = "Aggiornamento avvio launcher ...";
                    text9 = "Recupero versione ...";
                    text10 = "Versione incompatibile. Recupero informazioni versione ...";
                    text11 = "Controllo differenze file. Server: {0} file Locale: {1} file";
                    text12 = "Aggiornamento del gioco...";
                    text13 = "Lettura della versione attualmente installata.";
                    text14 = "Sunshine minimizzato";
                    text15 = "Sunshine è ora nella barra delle applicazioni";
                    text17 = "Controllo modalità file.";
                    text18 = "Verifica file non riuscita.";
                    text19 = "Un file sconosciuto è occupato. Chiudi tutti i client e riavvia l'aggiornamento!";
                    text21 = "Il file {0} è occupato. Chiudi il client e riavvia l'aggiornamento!";
                    text22 = "Verifica dei file non riuscita!";
                    text23 = "Impossibile recuperare l'ultima versione del gioco!";
                    text24 = "Il controllo versione inizierà al termine dell'operazione attiva";
                    break;
                case "BR":
                case "PT":
                    text1 = "Erro ao atualizar a interface.";
                    text3 = "Não foi possível reiniciar o launcher!";
                    text4 = "%";
                    text5 = "{0} ficheiros para processar";
                    text6 = "Transferência das atualizações concluída.";
                    text7 = "A transferir ficheiros ...";
                    text8 = "A atualizar o arranque do launcher ...";
                    text9 = "A obter a versão ...";
                    text10 = "Incompatibilidade de versão. A obter as informações da versão ...";
                    text11 = "A verificar diferenças de ficheiros. Servidor: {0} ficheiro(s) Local: {1} ficheiro(s)";
                    text12 = "A atualizar o jogo...";
                    text13 = "A ler a versão atualmente instalada.";
                    text14 = "Sunshine minimizado";
                    text15 = "Sunshine está agora na barra de tarefas";
                    text17 = "A verificar o modo dos ficheiros.";
                    text18 = "A verificação dos ficheiros falhou.";
                    text19 = "Um ficheiro desconhecido está ocupado. Feche todos os clientes e execute a atualização novamente!";
                    text21 = "O ficheiro {0} está ocupado. Feche o cliente e execute a atualização novamente!";
                    text22 = "A verificação dos ficheiros falhou!";
                    text23 = "Falha ao obter a versão mais recente do jogo!";
                    text24 = "A verificação da versão começa quando a operação ativa terminar";
                    break;
                case "JP":
                    text1 = "インターフェースの更新に失敗しました。";
                    text3 = "ランチャーを再起動できません。";
                    text4 = "%";
                    text5 = "処理するファイル数: {0}";
                    text6 = "アップデートのダウンロードが完了しました。";
                    text7 = "ファイルをダウンロード中 ...";
                    text8 = "ランチャー起動データを更新中 ...";
                    text9 = "バージョンを取得中 ...";
                    text10 = "バージョン不一致。バージョン情報を取得中 ...";
                    text11 = "ファイル差分を確認中。サーバー: {0} ファイル / ローカル: {1} ファイル";
                    text12 = "ゲームを更新中...";
                    text13 = "現在インストールされているバージョンを読み込み中。";
                    text14 = "Sunshine を最小化しました";
                    text15 = "Sunshine はタスクバーにあります";
                    text17 = "ファイルモードを確認中。";
                    text18 = "ファイル確認に失敗しました。";
                    text19 = "不明なファイルが使用中です。すべてのクライアントを閉じてから、もう一度アップデートしてください。";
                    text21 = "ファイル {0} は使用中です。クライアントを閉じてから、もう一度アップデートしてください。";
                    text22 = "ファイル確認に失敗しました。";
                    text23 = "最新のゲームバージョンを取得できませんでした。";
                    text24 = "現在の処理が終わるとバージョン確認が始まります";
                    break;
                case "NL":
                    text1 = "Fout bij het bijwerken van de interface.";
                    text3 = "Kan de launcher niet opnieuw starten!";
                    text4 = "%";
                    text5 = "{0} bestanden te verwerken";
                    text6 = "Download van updates voltooid.";
                    text7 = "Bestanden worden gedownload ...";
                    text8 = "Launcher-opstart wordt bijgewerkt ...";
                    text9 = "Versie wordt opgehaald ...";
                    text10 = "Versie komt niet overeen. Versie-informatie wordt opgehaald ...";
                    text11 = "Bestandsverschillen controleren. Server: {0} bestand(en) Lokaal: {1} bestand(en)";
                    text12 = "Spel wordt bijgewerkt...";
                    text13 = "De momenteel geïnstalleerde versie wordt gelezen.";
                    text14 = "Sunshine geminimaliseerd";
                    text15 = "Sunshine staat nu in de taakbalk";
                    text17 = "Bestandsmodus controleren.";
                    text18 = "Bestandscontrole mislukt.";
                    text19 = "Een onbekend bestand is in gebruik. Sluit alle clients en start de update opnieuw!";
                    text21 = "Bestand {0} is in gebruik. Sluit de client en start de update opnieuw!";
                    text22 = "Bestandsverificatie mislukt!";
                    text23 = "De nieuwste spelversie kon niet worden opgehaald!";
                    text24 = "De versiecontrole start zodra de actieve bewerking is voltooid";
                    break;
                case "RU":
                    text1 = "Ошибка при обновлении интерфейса.";
                    text3 = "Не удалось перезапустить лаунчер!";
                    text4 = "%";
                    text5 = "Файлов для обработки: {0}";
                    text6 = "Загрузка обновлений завершена.";
                    text7 = "Идёт загрузка файлов ...";
                    text8 = "Обновление запуска лаунчера ...";
                    text9 = "Получение версии ...";
                    text10 = "Несовместимая версия. Получение информации о версии ...";
                    text11 = "Проверка различий файлов. Сервер: {0} файл(ов), локально: {1} файл(ов)";
                    text12 = "Идёт обновление игры...";
                    text13 = "Чтение установленной версии.";
                    text14 = "Sunshine свернут";
                    text15 = "Sunshine теперь находится на панели задач";
                    text17 = "Проверка режима файлов.";
                    text18 = "Проверка файлов не удалась.";
                    text19 = "Неизвестный файл занят. Закройте все клиенты и запустите обновление снова!";
                    text21 = "Файл {0} занят. Закройте клиент и запустите обновление снова!";
                    text22 = "Проверка файлов не удалась!";
                    text23 = "Не удалось получить последнюю версию игры!";
                    text24 = "Проверка версии начнётся после завершения активной операции";
                    break;
                default:
                    text1 = "Erreur lors de la mise interface.";
                    text3 = "Impossible de redémarrer le lanceur!";
                    text4 = "%";
                    text5 = "{0} fichiers à traiter";
                    text6 = "Téléchargement des mises a jour terminé.";
                    text7 = "Téléchargement des fichiers en cours ...";
                    text8 = "Mise à jour du démarrage ...";
                    text9 = "Récupération de la version ...";
                    text10 = "Incompatibilite de version. Récupération des informations de version ...";
                    text11 = "Vérification de la différence de fichier. Serveur: {0} fichier(s) Local {1} fichier(s)";
                    text12 = "Mise à jour du jeu en cours...";
                    text13 = "Lecture de la version actuellement installer.";
                    text14 = "Sunshine minimisé";
                    text15 = "Sunshine sera ici dans la barre des tâches";
                    text17 = "Vérification du mode fichiers.";
                    text18 = "La vérification des fichiers a échoué.";
                    text19 = "Fichier inconnu occupé. Fermez tous les clients et lancez la mise à jour à nouveau s'il vous plaît!";
                    text21 = "Fichier {0} occupé. Fermez le client et lancez le programme de mise à jour à nouveau!";
                    text22 = "La vérification des fichiers a échoué!";
                    text23 = "Vérifier la dernière version du jeu échoue pour une raison quelconque!";
                    text24 = "Le contrôle de la version commence après que l'opération active soit terminée";
                    break;
            }

            this.UpdateNewsAndLogsTabLabels();
            this.RefreshPlayButtonIdleState();
            this.RefreshVersionCheckButtonIdleState();

            if (this.newsList != null)
            {
                this.LoadNewsFeed();
            }
        }

        private void savelang()
        {
            string code = this.GetNormalizedLanguageCode();
            string langValue;
            string bindsValue;

            switch (code)
            {
                case "FR":
                    langValue = "fr";
                    bindsValue = "frFR";
                    break;
                case "EN":
                case "US":
                    langValue = "en";
                    bindsValue = "enGB";
                    break;
                case "DE":
                    langValue = "de";
                    bindsValue = "deDE";
                    break;
                case "ES":
                    langValue = "es";
                    bindsValue = "esES";
                    break;
                case "IT":
                    langValue = "it";
                    bindsValue = "itIT";
                    break;
                case "PT":
                case "BR":
                    langValue = "pt";
                    bindsValue = "ptPT";
                    break;
                case "JA":
                case "JP":
                    langValue = "ja";
                    bindsValue = "jaJP";
                    break;
                case "NL":
                    langValue = "nl";
                    bindsValue = "nlNL";
                    break;
                case "RU":
                    langValue = "ru";
                    bindsValue = "ruRU";
                    break;
                default:
                    langValue = "fr";
                    bindsValue = "frFR";
                    break;
            }

            Directory.CreateDirectory("data/Launcher");
            File.WriteAllText("data/Launcher/lang.txt", code.ToLowerInvariant());

            if (File.Exists("config.xml"))
            {
                string xml = File.ReadAllText("config.xml");
                xml = System.Text.RegularExpressions.Regex.Replace(
                    xml,
                    "<entry key=\"lang.current\">.*?</entry>",
                    "<entry key=\"lang.current\">" + langValue + "</entry>");

                xml = System.Text.RegularExpressions.Regex.Replace(
                    xml,
                    "<entry key=\"binds.current\">.*?</entry>",
                    "<entry key=\"binds.current\">" + bindsValue + "</entry>");

                File.WriteAllText("config.xml", xml);
            }

            checklang();
        }

        private void LoadBackgrounds()
        {
            if (!Directory.Exists("data/Launcher/sliders"))
            {
                Directory.CreateDirectory("data/Launcher/sliders");
            }

            string[] images = Directory.GetFiles("data/Launcher/sliders");
            foreach (string name in images)
            {
                this.backgroundsList.Add(name);
            }

            if (this.backgroundsList.Count > 0)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Directory.GetCurrentDirectory() + "/" + this.backgroundsList[0]);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                this.currentBackgroundIndex = 0;
            }
        }

        public void gerarradio()
        {
            List<Control> ctls = new List<Control>();
            for (int a = 0; a <= this.backgroundsList.Count - 1; a++)
            {
                RadioButton rb = new RadioButton()
                {
                    Content = "",
                    GroupName = "a",
                    Width = 20,
                    Height = 20,
                    BorderBrush = Brushes.Transparent,
                    Style = this.FindResource("MyRadio") as Style,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                if (cor == false)
                    rb.Foreground = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/UI/slider/pagination_hover.png")));
                else
                    rb.Foreground = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/UI/slider/pagination_hover2.png")));

                if (cor == false)
                    rb.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/UI/slider/pagination.png")));
                else
                    rb.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/UI/slider/pagination2.png")));

                rb.Click += Pagination_click;
                sp.Children.Add(rb);
            }

            if (this.backgroundsList.Count != 0)
                ((RadioButton)this.sp.Children[0]).IsChecked = true;
        }

        public void ChangeBackground(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (this.backgroundsList.Count > 0)
            {
                if (this.backgroundsList.ElementAtOrDefault(this.currentBackgroundIndex + 1) != null)
                {
                    this.currentBackgroundIndex++;
                }
                else
                {
                    this.currentBackgroundIndex = 0;
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    ((RadioButton)this.sp.Children[this.currentBackgroundIndex]).IsChecked = true;
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(Directory.GetCurrentDirectory() + "/" + this.backgroundsList[this.currentBackgroundIndex]);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }), DispatcherPriority.Background);
            }
        }

        private void ChangeBackgroundback()
        {
            if (this.backgroundsList.Count > 0)
            {
                if (this.backgroundsList.ElementAtOrDefault(this.currentBackgroundIndex - 1) != null)
                {
                    this.currentBackgroundIndex--;
                }
                else
                {
                    this.currentBackgroundIndex = this.backgroundsList.Count - 1;
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    ((RadioButton)this.sp.Children[this.currentBackgroundIndex]).IsChecked = true;
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(Directory.GetCurrentDirectory() + "/" + this.backgroundsList[this.currentBackgroundIndex]);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }), DispatcherPriority.Background);
            }
        }

        private void ChangeBackgroundnext()
        {
            if (this.backgroundsList.Count > 0)
            {
                if (this.backgroundsList.ElementAtOrDefault(this.currentBackgroundIndex + 1) != null)
                {
                    this.currentBackgroundIndex++;
                }
                else
                {
                    this.currentBackgroundIndex = 0;
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    ((RadioButton)this.sp.Children[this.currentBackgroundIndex]).IsChecked = true;
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(Directory.GetCurrentDirectory() + "/" + this.backgroundsList[this.currentBackgroundIndex]);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }), DispatcherPriority.Background);
            }
        }

        private void ChangeBackgroundradio()
        {
            for (int i = 0; i < this.sp.Children.Count; i++)
                if ((bool)((RadioButton)this.sp.Children[i]).IsChecked)
                    this.currentBackgroundIndex = i;

            Dispatcher.Invoke(new Action(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Directory.GetCurrentDirectory() + "/" + this.backgroundsList[this.currentBackgroundIndex]);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            }), DispatcherPriority.Background);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        void MyNotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.RestoreFromTray();
        }

        private void Close_click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Pagination_click(object sender, RoutedEventArgs e)
        {
            ChangeBackgroundradio();
            timer.Enabled = false;
            timer.Enabled = true;
        }

        private void Reduce_click(object sender, RoutedEventArgs e)
        {
            this.MinimizeToTray(true);
        }

        private void Prev_click(object sender, RoutedEventArgs e)
        {
            this.ChangeBackgroundback();
            timer.Enabled = false;
            timer.Enabled = true;
        }

        private void Next_click(object sender, RoutedEventArgs e)
        {
            this.ChangeBackgroundnext();
            timer.Enabled = false;
            timer.Enabled = true;
        }

        private void option_click(object sender, RoutedEventArgs e)
        {
            checkcor();
            savecor();
        }

        private void Lang_click(object sender, RoutedEventArgs e)
        {
            this.ShowLanguageMenu(this.langList.Visibility != Visibility.Visible);
        }

        private void Play_click(object sender, RoutedEventArgs e)
        {
            this.LaunchDofus(true);
        }

        private void closeButtonEnter(object sender, MouseEventArgs e)
        {
            this.SetImage(this.closeButton, "pack://application:,,,/UI/Frames/Close/2.png");
        }

        private void closeButtonLeaver(object sender, MouseEventArgs e)
        {
            this.SetImage(this.closeButton, "pack://application:,,,/UI/Frames/Close/1.png");
        }

        private void closeButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.SetImage(this.closeButton, "pack://application:,,,/UI/Frames/Close/3.png");
        }

        private void closeButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.SetImage(this.closeButton, this.closeButton.IsMouseOver ? "pack://application:,,,/UI/Frames/Close/2.png" : "pack://application:,,,/UI/Frames/Close/1.png");
        }

        private void reduceButtonEnter(object sender, MouseEventArgs e)
        {
            this.SetImage(this.reduceButton, "pack://application:,,,/UI/Frames/Mini/2.png");
        }

        private void reduceButtonLeaver(object sender, MouseEventArgs e)
        {
            this.SetImage(this.reduceButton, "pack://application:,,,/UI/Frames/Mini/1.png");
        }

        private void reduceButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.SetImage(this.reduceButton, "pack://application:,,,/UI/Frames/Mini/3.png");
        }

        private void reduceButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.SetImage(this.reduceButton, this.reduceButton.IsMouseOver ? "pack://application:,,,/UI/Frames/Mini/2.png" : "pack://application:,,,/UI/Frames/Mini/1.png");
        }

        private void playButtonEnter(object sender, MouseEventArgs e)
        {
            this.RefreshPlayButtonHoverState();
        }

        private void playButtonLeaver(object sender, MouseEventArgs e)
        {
            this.RefreshPlayButtonIdleState();
        }

        private void playButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.RefreshPlayButtonPressedState();
        }

        private void playButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.playButton.IsMouseOver)
            {
                this.RefreshPlayButtonHoverState();
            }
            else
            {
                this.RefreshPlayButtonIdleState();
            }
        }
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._isUpdatingLanguageSelection)
            {
                return;
            }

            FlagItem selectedFlag = this.langList.SelectedItem as FlagItem;
            if (selectedFlag == null)
            {
                return;
            }

            lang = selectedFlag.Code.ToLowerInvariant();
            savelang();
            if (cor == false)
            {
                cor = true;
                checkcor();
            }
            else
            {
                cor = false;
                checkcor();
            }

            checklang();
            this.ShowLanguageMenu(false);
        }

        private void ListView_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void NewsTabButton_Click(object sender, RoutedEventArgs e)
        {
            this.ShowNewsTab();
        }

        private void LogsTabButton_Click(object sender, RoutedEventArgs e)
        {
            this.ShowLogsTab();
        }

        private void newsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NewsItem item = this.newsList.SelectedItem as NewsItem;
            if (item == null || string.IsNullOrWhiteSpace(item.Link))
            {
                return;
            }

            try
            {
                Process.Start(item.Link);
            }
            catch
            {
            }
        }

        private void Window_StateChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void VersionCHKButtonEnter(object sender, MouseEventArgs e)
        {
            this.RefreshVersionCheckButtonHoverState();
        }

        private void VersionCHKButtonLeaver(object sender, MouseEventArgs e)
        {
            this.RefreshVersionCheckButtonIdleState();
        }

        private void VersionCHKButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.RefreshVersionCheckButtonPressedState();
        }

        private void VersionCHKButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.VersionCHKButton.IsMouseOver)
            {
                this.RefreshVersionCheckButtonHoverState();
            }
            else
            {
                this.RefreshVersionCheckButtonIdleState();
            }
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("");
        }

        private void VersionCHKButton_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateWorkerInstance.Close();
            this.UpdateWorkerInstance.Unpause();
            this.Commands.Clear();
            this.Commands.Enqueue(CommandQueue.RunUpdater);
            if (this.UpdateWorkerInstance.IsBusy)
            {
                this.LogStatusOnly(text24);
            }
            else
            {
                this.ProcessQueueCommand();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            timer.Enabled = false;
            if (MyNotifyIcon != null)
            {
                MyNotifyIcon.Visible = false;
                if (MyNotifyIcon.ContextMenuStrip != null)
                {
                    MyNotifyIcon.ContextMenuStrip.Dispose();
                }
                MyNotifyIcon.Dispose();
            }

            base.OnClosed(e);
        }

        private TextBlock NewsTabTextBlock
        {
            get { return ((Grid)this.newsTabButton.Content).Children.OfType<TextBlock>().First(); }
        }

        private TextBlock LogsTabTextBlock
        {
            get { return ((Grid)this.logsTabButton.Content).Children.OfType<TextBlock>().First(); }
        }
    }
}
