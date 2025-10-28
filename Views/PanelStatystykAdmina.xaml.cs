using DocumentManagerApp.Data;
using DocumentManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

namespace DocumentManagerApp.Views
{
    public partial class PanelStatystykAdmina : Window
    {
        public PanelStatystykAdmina()
        {
            InitializeComponent();
            ShowStatsView();
        }
        private void ShowStatsView()
        {
            var statsView = new AdminStatsView();
            statsView.OnManageUsers = ShowUsersView;
            statsView.OnOpenLogs = ShowLogsView;
            statsView.OnClose = () => Close();
            MainContent.Content = statsView;
        }
        private void ShowUsersView()
        {
            var usersView = new UsersManagementView();
            usersView.OnBack = ShowStatsView;
            MainContent.Content = usersView;
        }
        private void ShowLogsView()
        {
            var logsView = new ChangeLogView();
            logsView.OnBack = ShowStatsView;
            MainContent.Content = logsView;
        }
    }
}


