using DocumentManagerApp.Data;
using DocumentManagerApp.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DocumentManagerApp.Models;


namespace TwojaNamespace
{
    public partial class SettingsWindow : Window
    {
        private static readonly string baseStoragePath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DocumentsStorage");
        private static readonly string reportsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Reports");

        public SettingsWindow()
        {
            InitializeComponent();
        }
        private void OpenBaseFolder_Click(object sender, RoutedEventArgs e)
        {
            string fullPath = System.IO.Path.GetFullPath(baseStoragePath);

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            Process.Start("explorer.exe", fullPath);
        }
        private void OpenReportFolder_Click(object sender, RoutedEventArgs e)
        {
            string fullPath = System.IO.Path.GetFullPath(reportsPath);

            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            Process.Start("explorer.exe", fullPath);
        }
    }
}


