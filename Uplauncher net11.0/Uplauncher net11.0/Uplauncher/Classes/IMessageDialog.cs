using System;

namespace Uplauncher.Classes
{
    public interface IMessageDialog : IDialog
    {
        string Message
        {
            get;
            set;
        }
    }
}
