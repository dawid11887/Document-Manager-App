using DocumentManagerApp.Data;
using DocumentManagerApp.Views;
using System.Text;
using System.Windows;

namespace DocumentManagerApp
{
    public partial class App : Application
    {
        public App()
        {
            PdfSharp.Fonts.GlobalFontSettings.FontResolver = new MyFontResolver();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            base.OnStartup(e);

            DatabaseHelper.InitializeDatabase();

            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }
    }
}
