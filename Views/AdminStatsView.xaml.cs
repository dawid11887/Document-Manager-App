using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DocumentManagerApp.Data;

namespace DocumentManagerApp.Views
{
    public partial class AdminStatsView : UserControl
    {
        public Action OnManageUsers { get; set; }
        public Action OnClose { get; set; }
        public Action OnOpenLogs { get; set; }
        public AdminStatsView()
        {
            InitializeComponent();
            LoadStatistics();
        }
        private void LoadStatistics()
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                int usersCount = 0;
                int objectsCount = 0;
                int documentsCount = 0;
                int documentsLast7Days = 0;
                int documentsLast30Days = 0;
                int objectsWithoutDocs = 0;
                string diskUsage = "";

                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Users", connection))
                    usersCount = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Objects", connection))
                    objectsCount = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Documents", connection))
                    documentsCount = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Documents WHERE DateAdded >= @date", connection))
                {
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.AddDays(-7));
                    documentsLast7Days = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Documents WHERE DateAdded >= @date", connection))
                {
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.AddDays(-30));
                    documentsLast30Days = Convert.ToInt32(cmd.ExecuteScalar());
                }

                objectsWithoutDocs = GetObjectsWithoutDocumentsCount();
                diskUsage = GetDocumentStorageUsage();

                UsersCountText.Text = $"Użytkownicy: {usersCount}";
                ObjectsCountText.Text = $"Obiekty: {objectsCount}";
                DocumentsCountText.Text = $"Dokumenty: {documentsCount}";
                Documents7DaysText.Text = $"Dokumenty w ostatnich 7 dniach: {documentsLast7Days}";
                Documents30DaysText.Text = $"Dokumenty w ostatnich 30 dniach: {documentsLast30Days}";
                ObjectsWithoutDocsTextBlock.Text = $"Obiekty bez dokumentów: {objectsWithoutDocs}";
                DiskUsageTextBlock.Text = diskUsage;
            }
        }
        private int GetObjectsWithoutDocumentsCount()
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            string query = @"SELECT COUNT(*) FROM Objects m LEFT JOIN Documents d ON m.Id = d.ObjectId WHERE d.Id IS NULL";
            using var cmd = new SQLiteCommand(query, connection);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        private string GetDocumentStorageUsage()
        {
            string folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentsStorage");
            if (!Directory.Exists(folderPath))
                return "Brak folderu";

            long totalBytes = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);

            double totalMB = totalBytes / (1024.0 * 1024.0);
            return $"Zajętość dysku: {totalMB:F2} MB";
        }
        private void ManageUsers_Click(object sender, RoutedEventArgs e) => OnManageUsers?.Invoke();
        private void CloseButton_Click(object sender, RoutedEventArgs e) => OnClose?.Invoke();
        private void OpenLogs_Click(object sender, RoutedEventArgs e) => OnOpenLogs?.Invoke();
    }
}
