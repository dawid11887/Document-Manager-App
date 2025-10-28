using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DocumentManagerApp
{
    public partial class CustomTitleBar : UserControl
    {
        private Rect _restoreBounds = new Rect(
            SystemParameters.WorkArea.Left + 100,
            SystemParameters.WorkArea.Top + 100,
            1200, // szerokość
            700   // wysokość
        );
        private bool _isMaximized = false;
        public CustomTitleBar()
        {
            InitializeComponent();
            UpdateMaximizeIcon();

            this.Loaded += (s, e) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.StateChanged += Window_StateChanged;
                }
            };
        }
        private void UpdateMaximizeIcon()
        {
            if (_isMaximized)
                MaximizeButton.Content = "\uE923"; // restore
            else
                MaximizeButton.Content = "\uE922"; // maximize
        }
        private void Window_StateChanged(object? sender, EventArgs e)
        {
            var window = sender as Window;
            if (window == null) return;

            if (window.WindowState == WindowState.Normal && !_isMaximized)
            {
                _restoreBounds = new Rect(window.Left, window.Top, window.Width, window.Height);
            }
        }
        public void MaximizeWindowToWorkAreaInitial()
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            _restoreBounds = new Rect(window.Left, window.Top, window.Width, window.Height);

            var workArea = SystemParameters.WorkArea;
            window.Left = workArea.Left;
            window.Top = workArea.Top;
            window.Width = workArea.Width;
            window.Height = workArea.Height;

            _isMaximized = true;
        }
        private void ToggleMaximizeRestore()
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Maximized;
            }
            else
            {
                window.WindowState = WindowState.Normal;
                window.Left = _restoreBounds.Left;
                window.Top = _restoreBounds.Top;
                window.Width = _restoreBounds.Width;
                window.Height = _restoreBounds.Height;
            }
            _isMaximized = window.WindowState == WindowState.Maximized;
            _isMaximized = !_isMaximized;
            UpdateMaximizeIcon();
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            if (e.ClickCount == 2)
            {
                ToggleMaximizeRestore();
                return;
            }
            if (_isMaximized)
            {
                Point cursorPosOnControl = e.GetPosition(this);
                Point screenPos = this.PointToScreen(cursorPosOnControl);

                double newLeft = screenPos.X - (_restoreBounds.Width * (cursorPosOnControl.X / this.ActualWidth));
                double newTop = screenPos.Y - cursorPosOnControl.Y; 

                var workArea = SystemParameters.WorkArea;
                if (newLeft < workArea.Left) newLeft = workArea.Left;
                if (newTop < workArea.Top) newTop = workArea.Top;

                window.Left = newLeft;
                window.Top = newTop;
                window.Width = _restoreBounds.Width;
                window.Height = _restoreBounds.Height;

                _isMaximized = false;

                try { window.DragMove(); }
                catch { /* czasami DragMove wyrzuci wyjątek; ignorujemy */ }
            }
            else
            {
                try { window.DragMove(); }
                catch { }
            }
        }
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            var w = Window.GetWindow(this);
            if (w != null) w.WindowState = WindowState.Minimized;
        }
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is MainWindow main)
            {
                main.LogoutButton_Click(main.LogoutButton, new RoutedEventArgs());
            }
            else
            {
                window?.Close();
            }
        }
    }
}
