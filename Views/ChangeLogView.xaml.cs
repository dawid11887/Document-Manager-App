using DocumentManagerApp.Data;
using DocumentManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DocumentManagerApp.Views
{
    public partial class ChangeLogView : UserControl
    {
        public Action OnBack { get; set; }

        public ChangeLogView()
        {
            InitializeComponent();
            LoadLogs();
        }
        private List<ChangeLog> allLogs = new List<ChangeLog>();

        private void LoadLogs()
        {
            allLogs.Clear();

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                string query = "SELECT UserName, Action, Target, Timestamp FROM ChangeLog ORDER BY Timestamp DESC";

                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var log = new ChangeLog
                        {
                            UserName = reader["UserName"]?.ToString() ?? "",
                            Action = reader["Action"]?.ToString() ?? "",
                            Target = reader["Target"]?.ToString() ?? ""
                        };

                        // Parsowanie Timestamp
                        var tsObj = reader["Timestamp"]?.ToString();
                        if (!string.IsNullOrEmpty(tsObj) && DateTime.TryParse(tsObj, out var dt))
                            log.Timestamp = dt;
                        else
                            log.Timestamp = DateTime.MinValue;

                        allLogs.Add(log);
                    }
                }
            }

            // Ustaw dane do wyświetlenia
            ChangeLogDataGrid.ItemsSource = new List<ChangeLog>(allLogs);
        }


        private void Back_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OnBack?.Invoke();
        }
        private void ApplyFilters()
        {
            if (ChangeLogDataGrid == null || ChangeLogDataGrid.ItemsSource == null)
                return;

            var filtered = new List<ChangeLog>(this.allLogs);

            string userFilter = UserFilterBox?.Text?.Trim()?.ToLower() ?? "";
            string actionFilter = (ActionFilterBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string dateFilter = (DateFilterBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (!string.IsNullOrEmpty(userFilter))
                filtered = filtered.Where(l => l.UserName.ToLower().Contains(userFilter)).ToList();

            if (actionFilter != null && actionFilter != "Wszystkie")
            {
                if (actionFilter == "Inne")
                {
                    // pokaż tylko akcje spoza standardowej listy
                    string[] knownActions = {
                        "Dodano obiekt",
                        "Edytowano obiekt",
                        "Usunięto obiekt",
                        "Dodano dokument",
                        "Dodano nową wersję dokumentu",
                        "Usunięto dokument (wszystkie wersje)"
                    };
                    filtered = filtered.Where(l => !knownActions.Contains(l.Action)).ToList();
                }
                else
                {
                    filtered = filtered.Where(l => l.Action.Contains(actionFilter)).ToList();
                }
            }

            if (dateFilter == "Ostatnie 7 dni")
                filtered = filtered.Where(l => l.Timestamp >= DateTime.Now.AddDays(-7)).ToList();
            else if (dateFilter == "Ostatnie 30 dni")
                filtered = filtered.Where(l => l.Timestamp >= DateTime.Now.AddDays(-30)).ToList();

            ChangeLogDataGrid.ItemsSource = filtered;
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            ApplyFilters();
        }
        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            UserFilterBox.Text = "";
            ActionFilterBox.SelectedIndex = 0;
            DateFilterBox.SelectedIndex = 0;
            ApplyFilters();
        }

    }
}
