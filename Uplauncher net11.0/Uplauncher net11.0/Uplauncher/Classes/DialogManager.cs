using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Uplauncher.Classes
{
    public class DialogManager : IDialogManager
    {
        private readonly Dispatcher _dispatcher;

        private readonly IDialogHost _dialogHost;

        public DialogManager(ContentControl parent, Dispatcher dispatcher)
        {
            this._dispatcher = dispatcher;
            this._dialogHost = new DialogLayeringHelper(parent);
        }

        public IMessageDialog CreateMessageDialog(string message, DialogMode dialogMode)
        {
            IMessageDialog dialog = null;
            this.InvokeInUIThread(delegate
            {
                dialog = new MessageDialog(this._dialogHost, dialogMode, message, this._dispatcher);
            });
            return dialog;
        }

        public IMessageDialog CreateMessageDialog(string message, string caption, DialogMode dialogMode)
        {
            IMessageDialog dialog = null;
            this.InvokeInUIThread(delegate
            {
                dialog = new MessageDialog(this._dialogHost, dialogMode, message, this._dispatcher)
                {
                    Caption = caption
                };
            });
            return dialog;
        }

        public ICustomContentDialog CreateCustomContentDialog(object content, DialogMode dialogMode)
        {
            ICustomContentDialog dialog = null;
            this.InvokeInUIThread(delegate
            {
                dialog = new CustomContentDialog(this._dialogHost, dialogMode, content, this._dispatcher);
            });
            return dialog;
        }

        public ICustomContentDialog CreateCustomContentDialog(object content, string caption, DialogMode dialogMode)
        {
            ICustomContentDialog dialog = null;
            this.InvokeInUIThread(delegate
            {
                dialog = new CustomContentDialog(this._dialogHost, dialogMode, content, this._dispatcher)
                {
                    Caption = caption
                };
            });
            return dialog;
        }

        private void InvokeInUIThread(Action del)
        {
            this._dispatcher.Invoke(del);
        }
    }
}
