using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Uplauncher.Classes;

namespace Uplauncher
{
    /// <summary>
    /// Interaction logic for DialogBaseControl.xaml
    /// </summary>
    internal partial class DialogBaseControl : UserControl, INotifyPropertyChanged, IComponentConnector
    {
        public DialogBaseControl(FrameworkElement originalContent, DialogBase dialog)
        {
            this.Caption = dialog.Caption;
            this.InitializeComponent();
            Image backgroundImage = originalContent.CaptureImage(false);
            backgroundImage.Stretch = Stretch.Fill;
            backgroundImage.Margin = new Thickness(backgroundImage.Margin.Left, backgroundImage.Margin.Top - 40.0, backgroundImage.Margin.Right, backgroundImage.Margin.Bottom);
            this.BackgroundImageHolder.Content = backgroundImage;
            this._dialog = dialog;
            this.CreateButtons();
        }

        public string Caption
        {
            get;
            private set;
        }

        public Visibility CaptionVisibility
        {
            get
            {
                return string.IsNullOrWhiteSpace(this.Caption) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public VerticalAlignment VerticalDialogAlignment
        {
            get
            {
                return this._verticalDialogAlignment;
            }
            set
            {
                this._verticalDialogAlignment = value;
                this.OnPropertyChanged("VerticalDialogAlignment");
            }
        }

        public HorizontalAlignment HorizontalDialogAlignment
        {
            get
            {
                return this._horizontalDialogAlignment;
            }
            set
            {
                this._horizontalDialogAlignment = value;
                this.OnPropertyChanged("HorizontalDialogAlignment");
            }
        }

        public void SetCustomContent(object content)
        {
            this.CustomContent.Content = content;
        }

        private void CreateButtons()
        {
            switch (this._dialog.Mode)
            {
                case DialogMode.None:
                    break;
                case DialogMode.Ok:
                    this.AddOkButton();
                    break;
                case DialogMode.Cancel:
                    this.AddCancelButton();
                    break;
                case DialogMode.OkCancel:
                    this.AddOkButton();
                    this.AddCancelButton();
                    break;
                case DialogMode.YesNo:
                    this.AddYesButton();
                    this.AddNoButton();
                    break;
                case DialogMode.YesNoCancel:
                    this.AddYesButton();
                    this.AddNoButton();
                    this.AddCancelButton();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void AddNoButton()
        {
            this.AddButton(this._dialog.NoText, this.GetCallback(this._dialog.No, DialogResultState.No), false, true, "CanNo");
        }

        public void AddYesButton()
        {
            this.AddButton(this._dialog.YesText, this.GetCallback(this._dialog.Yes, DialogResultState.Yes), true, false, "CanYes");
        }

        public void AddCancelButton()
        {
            this.AddButton(this._dialog.CancelText, this.GetCallback(this._dialog.Cancel, DialogResultState.Cancel), false, true, "CanCancel");
        }

        public void AddOkButton()
        {
            this.AddButton(this._dialog.OkText, this.GetCallback(this._dialog.Ok, DialogResultState.Ok), true, true, "CanOk");
        }

        private void AddButton(string buttonText, Action callback, bool isDefault, bool isCancel, string bindingPath)
        {
            Button btn = new Button
            {
                Content = buttonText,
                MinWidth = 80.0,
                MaxWidth = 150.0,
                IsDefault = isDefault,
                IsCancel = isCancel,
                Margin = new Thickness(5.0)
            };
            Binding enabledBinding = new Binding(bindingPath)
            {
                Source = this._dialog
            };
            btn.SetBinding(UIElement.IsEnabledProperty, enabledBinding);
            btn.Click += delegate (object s, RoutedEventArgs e)
            {
                callback();
            };
            this.ButtonsGrid.Columns++;
            this.ButtonsGrid.Children.Add(btn);
        }

        internal void RemoveButtons()
        {
            this.ButtonsGrid.Children.Clear();
        }

        private Action GetCallback(Action dialogCallback, DialogResultState result)
        {
            this._dialog.Result = result;
            return delegate
            {
                if (dialogCallback != null)
                {
                    dialogCallback();
                }
                if (this._dialog.CloseBehavior == DialogCloseBehavior.AutoCloseOnButtonClick)
                {
                    this._dialog.Close();
                }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this._dialog.Result = DialogResultState.Cancel;
            this._dialog.Close();
        }
        private readonly DialogBase _dialog;

        private VerticalAlignment _verticalDialogAlignment = VerticalAlignment.Center;

        private HorizontalAlignment _horizontalDialogAlignment = HorizontalAlignment.Center;
    }
}
