using System;
using System.Windows.Threading;

namespace Uplauncher.Classes
{
    internal class CustomContentDialog : DialogBase, ICustomContentDialog, IDialog
    {
        public CustomContentDialog(IDialogHost dialogHost, DialogMode dialogMode, object content, Dispatcher dispatcher) : base(dialogHost, dialogMode, dispatcher)
        {
            base.SetContent(content);
        }
    }
}
