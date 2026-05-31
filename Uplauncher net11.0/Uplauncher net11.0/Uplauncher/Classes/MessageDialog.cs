using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Uplauncher.Classes
{
    internal class MessageDialog : DialogBase, IMessageDialog, IDialog
    {
        private TextBlock _messageTextBlock;

        public MessageDialog(IDialogHost dialogHost, DialogMode dialogMode, string message, Dispatcher dispatcher) : base(dialogHost, dialogMode, dispatcher)
        {
            MessageDialog dialog = this;
            base.InvokeUICall(delegate
            {

                dialog._messageTextBlock = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                dialog.SetContent(dialog._messageTextBlock);
            });
        }

        public string Message
        {
            get
            {
                string text = string.Empty;
                base.InvokeUICall(delegate
                {
                    text = this._messageTextBlock.Text;
                });
                return text;
            }
            set
            {
                base.InvokeUICall(delegate
                {
                    this._messageTextBlock.Text = value;
                });
            }
        }
    }
}
