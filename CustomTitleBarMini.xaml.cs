using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DocumentManagerApp
{
    public partial class CustomTitleBarMini : UserControl
    {
        public CustomTitleBarMini()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;
    
            window.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            
            if (window is MainWindow main)
            {
                main.LogoutButton_Click(main.LogoutButton, new RoutedEventArgs());
            }
            else
                Window.GetWindow(this).Close();
        }
    }
}
