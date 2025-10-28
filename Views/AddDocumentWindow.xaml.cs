using DocumentManagerApp.Data;
using DocumentManagerApp.Helpers;
using DocumentManagerApp.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DocumentManagerApp.Views
{
    public partial class AddDocumentWindow : Window
    {
        private string selectedFilePath;
        private readonly string storageDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentsStorage");
        public Action OnDataChanged { get; set; }
        public int? NewlyAddedDocumentObjectId { get; private set; }

        private readonly Models.Object _targetObject;
        private readonly Document _documentToVersion;
        public AddDocumentWindow(Models.Object targetObject)
        {
            InitializeComponent();
            _targetObject = targetObject;
            _documentToVersion = null;
            TargetObjectText.Text = $"Dodajesz nowy dokument do obiektu: {_targetObject.Name}";
            UpdateFileSelectionUI();
        }
        public AddDocumentWindow(Document documentToVersion)
        {
            InitializeComponent();
            _documentToVersion = documentToVersion; 

            _targetObject = DatabaseHelper.GetObjectById(_documentToVersion.ObjectId);

            if (_targetObject == null)
            {
                MessageBox.Show($"Nie można znaleźć obiektu o ID: {_documentToVersion.ObjectId}", "Błąd danych", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }
            TargetObjectText.Text = $"Dodajesz nową wersję (v{_documentToVersion.Version + 1}) dla:";
            SelectedFileNameText.Text = System.IO.Path.GetFileName(_documentToVersion.FilePath);
            SelectedFileNameText.Opacity = 0.7;

            SetSelectedCategory(_documentToVersion.Category);
            CategoryComboBox.IsEnabled = false;

            SelectFileButton.Content = "Wybierz NOWY plik PDF";
            UpdateFileSelectionUI();
        }
        private void UpdateFileSelectionUI()
        {
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                SelectedFileNameText.Text = System.IO.Path.GetFileName(selectedFilePath);
                BeforeDropPanel.Visibility = Visibility.Collapsed;
                AfterDropPanel.Visibility = Visibility.Visible;
            }
            else
            {
                BeforeDropPanel.Visibility = Visibility.Visible;
                AfterDropPanel.Visibility = Visibility.Collapsed;

                if (_documentToVersion != null)
                {
                    SelectedFileNameText.Text = System.IO.Path.GetFileName(_documentToVersion.FilePath);
                }
            }
        }
        private void SetSelectedCategory(string categoryName)
        {
            foreach (ComboBoxItem item in CategoryComboBox.Items)
            {
                if (item.Content.ToString() == categoryName)
                {
                    CategoryComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Pliki PDF (*.pdf)|*.pdf";
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                UpdateFileSelectionUI();
            }
        }
        // ================== LOGIKA DRAG & DROP ==================
        // Metoda wywoływana, gdy plik jest przeciągany NAD obszar okna
        private void DropTarget_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                DropOverlay.Visibility = Visibility.Visible;
                e.Effects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }
        // Metoda wywoływana, gdy kursor z plikiem opuszcza obszar okna
        private void DropTarget_PreviewDragLeave(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }
        // Metoda wywoływana, gdy plik jest UPUSZCZANY na obszar okna
        private void DropTarget_Drop(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 1)
                {
                    MessageBox.Show("Można dodać tylko jeden plik na raz.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string filePath = files[0];

                if (System.IO.Path.GetExtension(filePath).ToLowerInvariant() != ".pdf")
                {
                    MessageBox.Show("Można dodawać tylko pliki w formacie PDF.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                selectedFilePath = filePath;
                UpdateFileSelectionUI();
            }
            e.Handled = true;
        }
        // =============================================================
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Wybierz plik PDF.");
                return;
            }
            if (_targetObject == null)
            {
                MessageBox.Show("Błąd: Obiekt docelowy nie został określony.", "Błąd wewnętrzny", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string baseStoragePath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DocumentsStorage");
            string objectFolder = System.IO.Path.Combine(baseStoragePath, $"Object_{_targetObject.Id}");

            if (!Directory.Exists(objectFolder))
                Directory.CreateDirectory(objectFolder);

            string fileName = System.IO.Path.GetFileName(selectedFilePath);
            string destinationPath = System.IO.Path.Combine(objectFolder, fileName);

            int counter = 1;
            string originalFileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string extension = System.IO.Path.GetExtension(fileName);
            while (File.Exists(destinationPath))
            {
                fileName = $"{originalFileName}_{counter++}{extension}";
                destinationPath = System.IO.Path.Combine(objectFolder, fileName);
            }
            File.Copy(selectedFilePath, destinationPath);
            string relativePath = System.IO.Path.Combine($"Object_{_targetObject.Id}", fileName);

            string selectedCategory = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Inne";

            if (_documentToVersion != null)
            {
                var newVersionDocument = new Document
                {
                    ObjectId = _targetObject.Id,
                    FilePath = relativePath,
                    DocumentType = "PDF",
                    DateAdded = DateTime.Now,
                    Category = selectedCategory,
                    Version = _documentToVersion.Version + 1,
                    IsActive = true,
                    VersionGroupId = _documentToVersion.VersionGroupId
                };
                DatabaseHelper.AddNewDocumentVersion(newVersionDocument, _documentToVersion.Id);

                DatabaseHelper.AddChangeLog(UserSession.CurrentUser.Username, "Dodano nową wersję dokumentu",
                    $"Obiekt: {_targetObject.Name}, Plik: {System.IO.Path.GetFileName(relativePath)}, Wersja: {newVersionDocument.Version}");

                this.NewlyAddedDocumentObjectId = _targetObject.Id;
            }
            else
            {
                var newDocument = new Document
                {
                    ObjectId = _targetObject.Id,
                    FilePath = relativePath,
                    DocumentType = "PDF",
                    DateAdded = DateTime.Now,
                    Category = selectedCategory,
                };
                DatabaseHelper.AddDocument(newDocument);

                DatabaseHelper.AddChangeLog(UserSession.CurrentUser.Username, "Dodano dokument",
                    $"Obiekt: {_targetObject.Name}, Plik: {System.IO.Path.GetFileName(relativePath)}");

                this.NewlyAddedDocumentObjectId = _targetObject.Id;
            }
            OnDataChanged?.Invoke();
            DialogResult = true;
            Close();
        }
    }
}