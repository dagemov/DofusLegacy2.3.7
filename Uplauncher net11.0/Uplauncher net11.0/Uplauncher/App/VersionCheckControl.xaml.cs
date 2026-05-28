using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Markup;
using Uplauncher.Classes;

namespace Uplauncher
{
    /// <summary>
    /// Interaction logic for VersionCheckControl.xaml
    /// </summary>
    public partial class VersionCheckControl : UserControl, IComponentConnector
    {
        public VersionCheckWorker VCheckWorkerInstance
        {
            get;
            set;
        }

        public IDialog Dialog
        {
            get;
            set;
        }

        public event Action WorkerComplete;

        public VersionCheckControl()
        {
            this.InitializeComponent();
            this.VCheckWorkerInstance = new VersionCheckWorker();
            this.VCheckWorkerInstance.ProgressChanged += new ProgressChangedEventHandler(this.VCheckWorkerInstance_ProgressChanged);
            this.VCheckWorkerInstance.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.VCheckWorkerInstance_RunWorkerCompleted);
            this.VCheckWorkerInstance.RunWorkerAsync();
        }

        private void VCheckWorkerInstance_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                switch (((VersionCheckWorker.VersionCheckWorkerProgressState)e.UserState).State)
                {
                    case VersionCheckWorker.VersionCheckWorkerState.Starting:
                        this.StatusProgressBar.Value = (double)e.ProgressPercentage;
                        break;
                    case VersionCheckWorker.VersionCheckWorkerState.GenerateVerInfo:
                        this.StatusProgressBar.Value = (double)e.ProgressPercentage;
                        break;
                    case VersionCheckWorker.VersionCheckWorkerState.Finished:
                        this.StatusProgressBar.Value = (double)e.ProgressPercentage;
                        break;
                }
            }
            catch
            {
            }
        }

        private void VCheckWorkerInstance_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.WorkerComplete != null)
            {
                this.WorkerComplete();
            }
            this.Dialog.Close();
        }
    }
}
