using System;
using System.ComponentModel;

namespace Uplauncher.Classes
{
    public class BaseBackgroundWorker : BackgroundWorker
    {
        public BaseBackgroundWorker()
        {
            base.WorkerSupportsCancellation = true;
            base.WorkerReportsProgress = true;
        }
    }
}
