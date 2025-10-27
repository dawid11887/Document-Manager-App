using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DocumentManagerApp
{
    public partial class CustomTitleBar : UserControl
    {
        // zapamiętane bounds do przywrócenia
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
            UpdateMaximizeIcon(); // żeby ikona była zgodna ze startowym stanem okna

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

            // zapamiętaj bieżące bounds jako restore (może być użyteczne)
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
                window.WindowState = WindowState.Maximized; // Windows + WM_GETMINMAXINFO -> ogranicza do WorkArea
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
                // podwójny klik -> toggle
                ToggleMaximizeRestore();
                return;
            }

            // pojedynczy przycisk myszy: start drag
            if (_isMaximized)
            {
                // zachowanie podobne do Windows: przy przeciąganiu z maksymalizacji
                // przywróć do zapisanych bounds, tak aby kursorem trzymać przycisk
                Point cursorPosOnControl = e.GetPosition(this);
                Point screenPos = this.PointToScreen(cursorPosOnControl);

                // przywracamy poprzedni rozmiar (z _restoreBounds) i ustawiamy Left tak, żeby kursor był mniej-więcej na tym samym miejscu.
                double newLeft = screenPos.X - (_restoreBounds.Width * (cursorPosOnControl.X / this.ActualWidth));
                double newTop = screenPos.Y - cursorPosOnControl.Y; // lekki offset

                // granice ekranu (proste zabezpieczenie)
                var workArea = SystemParameters.WorkArea;
                if (newLeft < workArea.Left) newLeft = workArea.Left;
                if (newTop < workArea.Top) newTop = workArea.Top;

                window.Left = newLeft;
                window.Top = newTop;
                window.Width = _restoreBounds.Width;
                window.Height = _restoreBounds.Height;

                _isMaximized = false;

                // teraz rozpocznij DragMove() — powinno zadziałać bo przycisk nadal wciśnięty
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
            // korzystamy z tej samej logiki toggle
            ToggleMaximizeRestore();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window is MainWindow main)
            {
                // wywołaj istniejącą logikę wylogowania
                main.LogoutButton_Click(main.LogoutButton, new RoutedEventArgs());
            }
            else
            {
                window?.Close();
            }
        }
    }
}
