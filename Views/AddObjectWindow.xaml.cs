using DocumentManagerApp.Data;
using DocumentManagerApp.Helpers;
using DocumentManagerApp.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
    public partial class AddObjectWindow : Window
    {
        private Models.Object editingObject = null;
        public Action OnDataChanged { get; set; }
        public AddObjectWindow()
        {
            InitializeComponent();
        }

        public AddObjectWindow(Models.Object objectToEdit) : this()
        {
            editingObject = objectToEdit;

            if (editingObject != null)
            {
                Title = "Edytuj obiekt";
                NameTextBox.Text = editingObject.Name;
                LocationTextBox.Text = editingObject.Location;
                DescriptionTextBox.Text = editingObject.Description;
                ProducentTextBox.Text = editingObject.Producent;
                ModelTextBox.Text = editingObject.Model;
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Nazwa obiektu jest wymagana.");
                return;
            }

            if (editingObject == null)
            {
                var newObject = new Models.Object
                {
                    Name = name,
                    Location = LocationTextBox.Text.Trim(),
                    Description = DescriptionTextBox.Text.Trim(),
                    Producent = ProducentTextBox.Text.Trim(),
                    Model = ModelTextBox.Text.Trim()
                };

                DatabaseHelper.AddObject(newObject);
                DatabaseHelper.AddChangeLog(UserSession.CurrentUser.Username, "Dodano obiekt", $"Objekt: {name}");
            }
            else
            {
                editingObject.Name = name;
                editingObject.Location = LocationTextBox.Text.Trim();
                editingObject.Description = DescriptionTextBox.Text.Trim();
                editingObject.Producent = ProducentTextBox.Text.Trim();
                editingObject.Model = ModelTextBox.Text.Trim();

                DatabaseHelper.UpdateObject(editingObject);
                DatabaseHelper.AddChangeLog(UserSession.CurrentUser.Username, "Edytowano obiekt", $"Objekt: {name}");
            }
            OnDataChanged?.Invoke();
            DialogResult = true;
            Close();
        }
    }
}

