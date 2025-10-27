using DocumentManagerApp.Data;
using DocumentManagerApp.Models;
using System.Collections.Generic;
using System.Windows;

namespace DocumentManagerApp.Views
{
    public partial class VersionHistoryWindow : Window
    {
        public VersionHistoryWindow(int versionGroupId, string originalFileName)
        {
            InitializeComponent();
            InfoWindow.Text = $"Historia wersji dla dokumentu: {originalFileName}";
            LoadHistory(versionGroupId);
        }
        private void LoadHistory(int versionGroupId)
        {
            List<Document> history = DatabaseHelper.GetDocumentVersionHistory(versionGroupId);
            VersionHistoryDataGrid.ItemsSource = history;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
