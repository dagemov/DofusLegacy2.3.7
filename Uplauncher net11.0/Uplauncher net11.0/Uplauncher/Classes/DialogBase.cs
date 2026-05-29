using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Uplauncher.Classes
{
    internal abstract class DialogBase : IDialog, INotifyPropertyChanged
    {
        protected DialogBase(IDialogHost dialogHost, DialogMode dialogMode, Dispatcher dispatcher)
        {
            this._dialogHost = dialogHost;
            this._dispatcher = dispatcher;
            this.Mode = dialogMode;
            this.CloseBehavior = DialogCloseBehavior.AutoCloseOnButtonClick;
            this.OkText = "Ok";
            this.CancelText = "Cancel";
            this.YesText = "Yes";
            this.NoText = "No";
            switch (dialogMode)
            {
                case DialogMode.None:
                    break;
                case DialogMode.Ok:
                    this.CanOk = true;
                    break;
                case DialogMode.Cancel:
                    this.CanCancel = true;
                    break;
                case DialogMode.OkCancel:
                    this.CanOk = true;
                    this.CanCancel = true;
                    break;
                case DialogMode.YesNo:
                    this.CanYes = true;
                    this.CanNo = true;
                    break;
                case DialogMode.YesNoCancel:
                    this.CanYes = true;
                    this.CanNo = true;
                    this.CanCancel = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dialogMode");
            }
        }

        protected DialogBaseControl DialogBaseControl
        {
            get;
            private set;
        }

        protected void SetContent(object content)
        {
            this._content = content;
        }

        protected void InvokeUICall(Action del)
        {
            this._dispatcher.Invoke(del, DispatcherPriority.DataBind);
        }

        public DialogMode Mode
        {
            get;
            private set;
        }

        public DialogResultState Result
        {
            get;
            set;
        }

        public DialogCloseBehavior CloseBehavior
        {
            get;
            set;
        }

        public Action Ok
        {
            get;
            set;
        }

        public Action Cancel
        {
            get;
            set;
        }

        public Action Yes
        {
            get;
            set;
        }

        public Action No
        {
            get;
            set;
        }

        public bool CanOk
        {
            get
            {
                return this._canOk;
            }
            set
            {
                this._canOk = value;
                this.OnPropertyChanged("CanOk");
            }
        }

        public bool CanCancel
        {
            get
            {
                return this._canCancel;
            }
            set
            {
                this._canCancel = value;
                this.OnPropertyChanged("CanCancel");
            }
        }

        public bool CanYes
        {
            get
            {
                return this._canYes;
            }
            set
            {
                this._canYes = value;
                this.OnPropertyChanged("CanYes");
            }
        }

        public bool CanNo
        {
            get
            {
                return this._canNo;
            }
            set
            {
                this._canNo = value;
                this.OnPropertyChanged("CanNo");
            }
        }

        public string OkText
        {
            get;
            set;
        }

        public string CancelText
        {
            get;
            set;
        }

        public string YesText
        {
            get;
            set;
        }

        public string NoText
        {
            get;
            set;
        }

        public string Caption
        {
            get;
            set;
        }

        public VerticalAlignment VerticalDialogAlignment
        {
            set
            {
                if (this.DialogBaseControl == null)
                {
                    this._verticalDialogAlignment = new VerticalAlignment?(value);
                }
                else
                {
                    this.DialogBaseControl.VerticalDialogAlignment = value;
                }
            }
        }

        public HorizontalAlignment HorizontalDialogAlignment
        {
            set
            {
                if (this.DialogBaseControl == null)
                {
                    this._horizontalDialogAlignment = new HorizontalAlignment?(value);
                }
                else
                {
                    this.DialogBaseControl.HorizontalDialogAlignment = value;
                }
            }
        }

        public void Show()
        {
            if (this.DialogBaseControl != null)
            {
                throw new Exception("The dialog can only be shown once.");
            }
            this.InvokeUICall(delegate
            {
                this.DialogBaseControl = new DialogBaseControl(this._dialogHost.GetCurrentContent(), this);
                this.DialogBaseControl.SetCustomContent(this._content);
                if (this._verticalDialogAlignment.HasValue)
                {
                    this.DialogBaseControl.VerticalDialogAlignment = this._verticalDialogAlignment.Value;
                }
                if (this._horizontalDialogAlignment.HasValue)
                {
                    this.DialogBaseControl.HorizontalDialogAlignment = this._horizontalDialogAlignment.Value;
                }
                this._dialogHost.ShowDialog(this.DialogBaseControl);
            });
        }

        public void Close()
        {
            if (this.DialogBaseControl != null)
            {
                this.Ok = null;
                this.Cancel = null;
                this.Yes = null;
                this.No = null;
                this.InvokeUICall(delegate
                {
                    this._dialogHost.HideDialog(this.DialogBaseControl);
                    this.DialogBaseControl.SetCustomContent(null);
                });
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private readonly IDialogHost _dialogHost;

        private readonly Dispatcher _dispatcher;

        private object _content;

        private bool _canOk;

        private bool _canCancel;

        private bool _canYes;

        private bool _canNo;

        private VerticalAlignment? _verticalDialogAlignment;

        private HorizontalAlignment? _horizontalDialogAlignment;
    }
}
