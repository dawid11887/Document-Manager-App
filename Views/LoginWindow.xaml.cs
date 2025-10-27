using DocumentManagerApp.Data;
using DocumentManagerApp.Helpers;
using DocumentManagerApp.Models;
using System;
using System.Data.SQLite;
using System.Windows;

namespace DocumentManagerApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text?.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Wprowadź nazwę użytkownika i hasło.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // W pliku Views/LoginWindow.xaml.cs, wewnątrz metody Login_Click
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT Id, Username, Password, Salt, Role FROM Users WHERE Username = @username LIMIT 1", connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var savedHash = reader["Password"]?.ToString();
                            var savedSalt = reader["Salt"]?.ToString();

                            // Sprawdzamy hasło nową, bezpieczną metodą
                            if (Helpers.PasswordHelper.VerifyPassword(password, savedHash, savedSalt))
                            {
                                var user = new User
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Username = reader["Username"].ToString(),
                                    Password = savedHash, // Przechowujemy hash, nie czysty tekst
                                    Role = reader["Role"].ToString()
                                };
                                UserSession.Login(user);
                                DialogResult = true;
                                Close();
                                return;
                            }
                        }
                    }
                }
            }

            MessageBox.Show("Nieprawidłowy login lub hasło", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
    }
}

