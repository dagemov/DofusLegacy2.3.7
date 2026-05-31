using System;

namespace Uplauncher.Classes
{
    public interface IDialogManager
    {
        IMessageDialog CreateMessageDialog(string message, DialogMode dialogMode);

        IMessageDialog CreateMessageDialog(string message, string caption, DialogMode dialogMode);

        ICustomContentDialog CreateCustomContentDialog(object content, DialogMode dialogMode);

        ICustomContentDialog CreateCustomContentDialog(object content, string caption, DialogMode dialogMode);
    }
}
