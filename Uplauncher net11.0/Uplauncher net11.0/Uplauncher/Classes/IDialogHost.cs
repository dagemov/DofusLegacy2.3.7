using System;
using System.Windows;

namespace Uplauncher.Classes
{
    internal interface IDialogHost
    {
        void ShowDialog(DialogBaseControl dialog);

        void HideDialog(DialogBaseControl dialog);

        FrameworkElement GetCurrentContent();
    }
}
