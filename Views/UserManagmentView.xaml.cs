using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using DocumentManagerApp.Data;
using DocumentManagerApp.Models;
using DocumentManagerApp.Helpers;

namespace DocumentManagerApp.Views
{
    public partial class UsersManagementView : UserControl
    {
        public Action OnBack { get; set; }
        public UsersManagementView()
        {
            InitializeComponent();
            LoadUsers();
        }
        private void LoadUsers()
        {
            var users = new List<User>();
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT Id, Username, Role FROM Users ORDER BY Username", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Username = reader["Username"].ToString(),
                            Role = reader["Role"].ToString()
                        });
                    }
                }
            }

            UsersList.ItemsSource = users;
        }
        private void Back_Click(object sender, RoutedEventArgs e) => OnBack?.Invoke();
        private User selectedUser;
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            selectedUser = (sender as FrameworkElement)?.DataContext as User;
            if (selectedUser == null) return;

            EditUserPanel.DataContext = selectedUser;

            EditUsernameText.Text = selectedUser.Username;
            EditRoleText.Text = selectedUser.Role;

            EditUserPanel.Visibility = Visibility.Visible;
            UsersList.Visibility = Visibility.Collapsed;
        }
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditUserPanel.Visibility = Visibility.Collapsed;
            UsersList.Visibility = Visibility.Visible;

            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            CancelPasswordReset_Click(sender, e);
        }
        private void SaveUserChanges_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null) return;

            CancelEdit_Click(null, null);
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var user = (sender as FrameworkElement)?.DataContext as User;
            if (user == null) return;

            if (UserSession.CurrentUser != null && user.Id == UserSession.CurrentUser.Id)
            {
                MessageBox.Show("Nie możesz usunąć konta, na które jesteś aktualnie zalogowany.", "Blokada", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show($"Usunąć użytkownika „{user.Username}”?", "Potwierdzenie",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM Users WHERE Id = @Id", connection))
                {
                    cmd.Parameters.AddWithValue("@Id", user.Id);
                    cmd.ExecuteNonQuery();
                }
            }
            LoadUsers();
        }
        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
                return;

            string newPass = NewPasswordBox.Password;
            string confirmPass = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPass))
            {
                MessageBox.Show("Nowe hasło nie może być puste.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("Podane hasła nie są takie same.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var (newHash, newSalt) = Helpers.PasswordHelper.HashPassword(newPass);
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                string query = "UPDATE Users SET Password = @Password, Salt = @Salt WHERE Username = @Username";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Password", newHash);
                    cmd.Parameters.AddWithValue("@Salt", newSalt);
                    cmd.Parameters.AddWithValue("@Username", selectedUser.Username);
                    cmd.ExecuteNonQuery();
                }
            }
            MessageBox.Show("Hasło zostało zresetowane pomyślnie.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

            CancelPasswordReset_Click(sender, e);
        }
        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPasswordButton.Visibility = Visibility.Collapsed;
            ChangePasswordPanel.Visibility = Visibility.Visible;

            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();

            NewPasswordBox.Focus();
        }
        private void CancelPasswordReset_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordPanel.Visibility = Visibility.Collapsed;
            ResetPasswordButton.Visibility = Visibility.Visible;

            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }
    public class FirstLetterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            return string.IsNullOrWhiteSpace(s) ? "?" : s.Trim()[0].ToString().ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class UsernameColorBrushConverter : IValueConverter
    {
        private static readonly Brush[] Palette =
        {
            new SolidColorBrush(Color.FromRgb(99,102,241)),   // indigo
            new SolidColorBrush(Color.FromRgb(59,130,246)),   // blue
            new SolidColorBrush(Color.FromRgb(16,185,129)),   // emerald
            new SolidColorBrush(Color.FromRgb(245,158,11)),   // amber
            new SolidColorBrush(Color.FromRgb(236,72,153)),   // pink
            new SolidColorBrush(Color.FromRgb(139,92,246)),   // violet
            new SolidColorBrush(Color.FromRgb(34,197,94)),    // green
            new SolidColorBrush(Color.FromRgb(2,132,199))     // sky
        };
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var name = value as string;
            if (string.IsNullOrWhiteSpace(name))
                return Brushes.Gray;

            int hash = name.Aggregate(23, (acc, c) => acc * 31 + c);
            return Palette[Math.Abs(hash) % Palette.Length];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
