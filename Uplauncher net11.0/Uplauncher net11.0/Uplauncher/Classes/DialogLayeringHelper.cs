using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Uplauncher.Classes
{
    internal class DialogLayeringHelper : IDialogHost
    {
        public DialogLayeringHelper(ContentControl parent)
        {
            this._parent = parent;
        }

        public bool HasDialogLayers
        {
            get
            {
                return this._layerStack.Any<object>();
            }
        }

        public void ShowDialog(DialogBaseControl dialog)
        {
            this._layerStack.Add(this._parent.Content);
            this._parent.Content = dialog;
        }

        public void HideDialog(DialogBaseControl dialog)
        {
            if (this._parent.Content == dialog)
            {
                object oldContent = this._layerStack.Last<object>();
                this._layerStack.Remove(oldContent);
                this._parent.Content = oldContent;
            }
            else
            {
                this._layerStack.Remove(dialog);
            }
        }

        public FrameworkElement GetCurrentContent()
        {
            return this._parent;
        }

        private readonly ContentControl _parent;

        private readonly List<object> _layerStack = new List<object>();
    }
}
