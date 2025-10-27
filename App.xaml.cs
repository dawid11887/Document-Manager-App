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
            // Ta część jest w porządku, dotyczy biblioteki PDFSharp
            PdfSharp.Fonts.GlobalFontSettings.FontResolver = new MyFontResolver();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Rejestracja kodowań, które iTextSharp potrzebuje (np. windows-1252)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            base.OnStartup(e);

            // USUWAMY STĄD WYWOŁANIE EnsureUsersTable()
            DatabaseHelper.InitializeDatabase();

            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                var mainWindow = new MainWindow();
                MainWindow = mainWindow; // <- To jest bardzo ważne
                mainWindow.Show();
            }
            else
            {
                Shutdown(); // login nieudany
            }
        }
    }
}
