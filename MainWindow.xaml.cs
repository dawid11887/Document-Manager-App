using DocumentManagerApp.Data;
using DocumentManagerApp.Helpers;
using DocumentManagerApp.Models;
using DocumentManagerApp.Views;
using Microsoft.Web.WebView2.Wpf;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TwojaNamespace;
using ModelDocument = DocumentManagerApp.Models.Document;

namespace DocumentManagerApp
{
    public partial class MainWindow : Window
    {
        private List<Document> currentObjectDocuments = new List<Document>();
        private readonly string connectionString = "Data Source=data.db";
        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;
            UpdateButtonStates();
            InitializeWebView();
            DatabaseHelper.InitializeDatabase();

            ObjectsDataGrid.SelectionChanged -= ObjectsDataGrid_SelectionChanged;
            ObjectsDataGrid.ItemsSource = GetObjects();
            ObjectsDataGrid.SelectionChanged += ObjectsDataGrid_SelectionChanged;

            // blokowanie przycisków dla zwykłego użytkownika
            if (!UserSession.IsAdmin)
            {
                AddObjectButton.Visibility = Visibility.Collapsed;
                DeleteObjectButton.Visibility = Visibility.Collapsed;
                EditObjectButton.Visibility = Visibility.Collapsed; 
                AddDocumentButton.Visibility = Visibility.Collapsed;
                DeleteDocumentButton.Visibility = Visibility.Collapsed; 
                AdminStatsButton.Visibility = Visibility.Collapsed;
            }
            if (UserSession.CurrentUser != null)
            {
                LoggedUserTextBlock.Text = $"Zalogowany jako: {UserSession.CurrentUser.Username} ({UserSession.CurrentUser.Role})";
            }
            if (UserSession.CurrentUser != null)
            {
                var username = UserSession.CurrentUser.Username;
                UserInitial.Text = username[0].ToString().ToUpper();
                UserAvatar.Background = new SolidColorBrush(GetRandomColor());
            }
        }
        private bool isWebViewReady = false;
        private async void InitializeWebView()
        {
            await PdfViewer.EnsureCoreWebView2Async(null);
            isWebViewReady = true;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TitleBar?.MaximizeWindowToWorkAreaInitial();
        }
        private void ReloadData()
        {
            var objects = GetObjects();
            ObjectsDataGrid.ItemsSource = objects;

            if (ObjectsDataGrid.SelectedItem is Models.Object selectedObject)
            {
                DocumentsDataGrid.ItemsSource = GetDocumentsForObject(selectedObject.Id);
            }
            else
            {
                DocumentsDataGrid.ItemsSource = null;
            }
        }
        private Document selectedDocument = null;
        private void DocumentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDocument = DocumentsDataGrid.SelectedItem as Document;

            if (selectedDocument == null)
            {
                PdfViewer.Visibility = Visibility.Collapsed;
                ClosePdfViewerButton.Visibility = Visibility.Collapsed;
            }
            UpdateButtonStates();
        }
        private void ShowPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentsDataGrid.SelectedItem is Document selectedDocument)
            {
                string documentsFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DocumentsStorage");
                string pdfPath = System.IO.Path.Combine(documentsFolder, selectedDocument.FilePath);

                if (System.IO.File.Exists(pdfPath))
                {
                    PdfViewer.Source = new Uri(pdfPath);
                    PdfViewer.Visibility = Visibility.Visible;
                    ClosePdfViewerButton.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Plik PDF nie został znaleziony:\n" + pdfPath);
                    PdfViewer.Visibility = Visibility.Collapsed;
                    ClosePdfViewerButton.Visibility = Visibility.Collapsed;
                }
            }
        }
        private Color GetRandomColor()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            byte r = (byte)rand.Next(50, 200);
            byte g = (byte)rand.Next(50, 200);
            byte b = (byte)rand.Next(50, 200);

            return Color.FromRgb(r, g, b);
        }
        public void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var resultConfirm = MessageBox.Show(
                "Czy na pewno chcesz się wylogować?",
                "Wylogowanie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultConfirm != MessageBoxResult.Yes)
                return;

            UserSession.Logout();

            var app = Application.Current;

            this.Hide();

            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true && UserSession.CurrentUser != null)
            {
                this.Close();

                var mainWindow = new MainWindow();
                app.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                app.Shutdown();
            }
        }
        private List<Models.Object> GetObjects()
        {
            return DatabaseHelper.GetAllObjects();
        }
        private void AddObjectButton_Click(object sender, RoutedEventArgs e)
        {
            var addObjectWindow = new DocumentManagerApp.Views.AddObjectWindow();
            addObjectWindow.Owner = this;
            addObjectWindow.OnDataChanged = RefreshMiniLog;
            if (addObjectWindow.ShowDialog() == true)
            {
                MessageBox.Show("Obiekt dodany.");
                ObjectsDataGrid.ItemsSource = GetObjects();
                ReloadData();
            }
        }
        private void AddDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsDataGrid.SelectedItem is not Models.Object selectedObject)
            {
                MessageBox.Show("Najpierw wybierz obiekt, do którego chcesz dodać dokument.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var addDocWindow = new DocumentManagerApp.Views.AddDocumentWindow(selectedObject);
            addDocWindow.OnDataChanged = RefreshMiniLog;

            if (addDocWindow.ShowDialog() == true)
            {
                MessageBox.Show("Dokument dodany.");

                int? objectId = addDocWindow.NewlyAddedDocumentObjectId;

                if (objectId.HasValue)
                {
                    if (ObjectsDataGrid.SelectedItem is Models.Object currentSelection && currentSelection.Id == objectId.Value)
                    {
                        LoadDocumentsForObject(objectId.Value);
                    }
                }
            }
        }
        //=======================================  FILTROWANIE DOKUMENTÓW  =====================================================
        private void ObjectsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ObjectsDataGrid.SelectedItem is Models.Object selectedObject)
            {
                currentObjectDocuments = GetDocumentsForObject(selectedObject.Id);
                DocumentSearchTextBox.Text = "";
                DocumentCategoryComboBox.SelectedIndex = 0;
                DocumentDateComboBox.SelectedIndex = 0;
                ApplyDocumentFilters();
            }
            else
            {
                currentObjectDocuments.Clear();
                DocumentsDataGrid.ItemsSource = null;
                PdfViewer.Visibility = Visibility.Collapsed;
                ClosePdfViewerButton.Visibility = Visibility.Collapsed;
            }
            UpdateButtonStates();
        }
        private void DocumentSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyDocumentFilters();
        }
        private void DocumentFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyDocumentFilters();
        }
        private void ApplyDocumentFilters()
        {
            if (DocumentsDataGrid == null || DocumentSearchTextBox == null
                || DocumentCategoryComboBox == null || DocumentDateComboBox == null)
                return;

            if (currentObjectDocuments == null || currentObjectDocuments.Count == 0)
            {
                DocumentsDataGrid.ItemsSource = null;
                return;
            }

            string nameFilter = DocumentSearchTextBox.Text.Trim().ToLower();
            string categoryFilter = (DocumentCategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Wszystkie";
            string dateFilter = (DocumentDateComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Wszystkie";

            var filtered = currentObjectDocuments.Where(d =>
                (string.IsNullOrEmpty(nameFilter) || (d.FilePath?.ToLower().Contains(nameFilter) ?? false)) &&
                (categoryFilter == "Wszystkie" || (d.Category ?? "").Equals(categoryFilter)) &&
                (dateFilter == "Wszystkie" || CheckDateFilter(d.DateAdded, dateFilter))
            ).ToList();

            DocumentsDataGrid.ItemsSource = filtered;
        }
        private bool CheckDateFilter(DateTime date, string filter)
        {
            switch (filter)
            {
                case "Ostatnie 7 dni":
                    return date >= DateTime.Now.AddDays(-7);
                case "Ostatnie 30 dni":
                    return date >= DateTime.Now.AddDays(-30);
                default:
                    return true;
            }
        }
        //======================================================================================================================
        private List<Document> GetDocumentsForObject(int objectId)
        {
            return DatabaseHelper.GetDocumentsForObject(objectId);
        }
        private void LoadDocumentsForObject(int objectId)
        {
            var documents = GetDocumentsForObject(objectId);
            DocumentsDataGrid.ItemsSource = documents;
        }
        // USUWANIE OBIEKTÓW
        private void DeleteObjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsDataGrid.SelectedItem is Models.Object selectedObject)
            {
                var result = MessageBox.Show($"Czy na pewno chcesz usunąć obiekt: {selectedObject.Name}?\nWszystkie jego dokumenty również zostaną usunięte z bazy danych.",
                                             "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DatabaseHelper.DeleteObjectAndAssociatedDocuments(selectedObject.Id);

                    DatabaseHelper.AddChangeLog(UserSession.CurrentUser.Username, "Usunięto obiekt", $"Obiekt: {selectedObject.Name}");
                    RefreshMiniLog();

                    string baseStoragePath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DocumentsStorage");
                    string objectFolder = System.IO.Path.Combine(baseStoragePath, $"Object_{selectedObject.Id}");

                    if (Directory.Exists(objectFolder))
                    {
                        if (Directory.EnumerateFileSystemEntries(objectFolder).Any())
                        {
                            var folderResult = MessageBox.Show($"Folder zawierający pliki dokumentów obiektu \"{selectedObject.Name}\" nadal zawiera dane.\n" +
                                                               $"Czy chcesz również usunąć jego zawartość z dysku?",
                                                               "Zawartość folderu", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (folderResult == MessageBoxResult.Yes)
                            {
                                try
                                {
                                    Directory.Delete(objectFolder, true);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Błąd przy usuwaniu folderu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Directory.Delete(objectFolder);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Błąd przy usuwaniu pustego folderu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    ObjectsDataGrid.ItemsSource = GetObjects();
                    ObjectsDataGrid.SelectedItem = null;
                    DocumentsDataGrid.ItemsSource = null;
                    MessageBox.Show(objectFolder);
                    ReloadData();
                    UpdateButtonStates();
                }
            }
            else
            {
                MessageBox.Show("Wybierz obiekt do usunięcia.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        // USUWANIE DOKUMENTOW
        private void DeleteDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentsDataGrid.SelectedItem is Document selectedDoc)
            {
                var result = MessageBox.Show($"Czy na pewno chcesz usunąć dokument '{selectedDoc.FileName}'\n***ORAZ WSZYSTKIE JEGO POPRZEDNIE WERSJE?***\n\nTej operacji nie będzie można cofnąć!",
                                              "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    int? groupId = selectedDoc.VersionGroupId;
                    List<string> filePathsToDelete = new List<string>();

                    if (groupId.HasValue)
                    {
                        filePathsToDelete = DatabaseHelper.GetFilePathsForVersionGroup(groupId);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(selectedDoc.FilePath))
                        {
                            filePathsToDelete.Add(selectedDoc.FilePath);
                        }
                    }

                    string baseStoragePath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DocumentsStorage");
                    foreach (string relativePath in filePathsToDelete)
                    {
                        if (string.IsNullOrEmpty(relativePath)) continue;

                        string fullFilePath = System.IO.Path.Combine(baseStoragePath, relativePath);
                        if (File.Exists(fullFilePath))
                        {
                            try
                            {
                                File.Delete(fullFilePath);
                                Console.WriteLine($"Usunięto plik: {fullFilePath}");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Nie udało się usunąć pliku fizycznego:\n{fullFilePath}\nBłąd: {ex.Message}", "Błąd usuwania pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Plik nie znaleziony (pomijanie): {fullFilePath}");
                        }
                    }
                    DatabaseHelper.DeleteDocumentAndAllVersions(groupId);

                    var objectDB = ObjectsDataGrid.ItemsSource?
                                 .OfType<Models.Object>()
                                 .FirstOrDefault(m => m.Id == selectedDoc.ObjectId);
                    string objectName = objectDB != null ? objectDB.Name : $"ID {selectedDoc.ObjectId}";

                    DatabaseHelper.AddChangeLog(
                        UserSession.CurrentUser.Username,
                        "Usunięto dokument (wszystkie wersje)",
                        $"Obiekt: {objectName}, Plik: {selectedDoc.FileName}"
                    );
                    RefreshMiniLog();

                    LoadDocumentsForObject(selectedDoc.ObjectId);
                    UpdateButtonStates();
                }
            }
            else
            {
                MessageBox.Show("Wybierz dokument do usunięcia.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        // EDYCJA MASZYN
        private void EditObjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsDataGrid.SelectedItem is Models.Object selectedObject)
            {
                var editWindow = new DocumentManagerApp.Views.AddObjectWindow(selectedObject);
                editWindow.Owner = this;
                editWindow.OnDataChanged = RefreshMiniLog;

                if (editWindow.ShowDialog() == true)
                {
                    DatabaseHelper.AddChangeLog(UserSession.CurrentUser.Username, "Edytowano obiekt", $"Obiekt: {selectedObject.Name}");
                    ObjectsDataGrid.ItemsSource = GetObjects();
                    DocumentsDataGrid.ItemsSource = null;
                    ReloadData();
                }
            }
            else
            {
                MessageBox.Show("Wybierz obiekt do edycji.");
            }
        }
        // WYSZUKIWARKA
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = SearchTextBox.Text.Trim().ToLower();

            var filteredObjects = GetObjects().Where(m =>
                (!string.IsNullOrEmpty(m.Name) && m.Name.ToLower().Contains(filter)) ||
                (!string.IsNullOrEmpty(m.Location) && m.Location.ToLower().Contains(filter))
            ).ToList();

            ObjectsDataGrid.ItemsSource = filteredObjects;
            DocumentsDataGrid.ItemsSource = null;
        }
        // PRZYCISK WYCZYŚĆ WYSZUKIWARKI
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            ObjectsDataGrid.ItemsSource = GetObjects();
            DocumentsDataGrid.ItemsSource = null;
        }
        // EKSPORT RAPORTU PDF
        private void ExportToPdfButton_Click(object sender, RoutedEventArgs e)
        {
            var objects = GetObjects();
            var documents = new List<ModelDocument>();
            foreach (var objectDB in objects)
            {
                documents.AddRange(GetDocumentsForObject(objectDB.Id));
            }
            string datePart = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            string fileName = $"Raport_{datePart}.pdf";
            string reportsFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Reports");
            System.IO.Directory.CreateDirectory(reportsFolder);
            string outputPath = System.IO.Path.Combine(reportsFolder, fileName);
            string logoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");

            try
            {
                using (var fs = new System.IO.FileStream(outputPath, System.IO.FileMode.Create))
                using (var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 36, 36))
                {
                    iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    // --- Czcionki ---
                    string fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    iTextSharp.text.pdf.BaseFont baseFont = iTextSharp.text.pdf.BaseFont.CreateFont(fontPath, iTextSharp.text.pdf.BaseFont.IDENTITY_H, iTextSharp.text.pdf.BaseFont.EMBEDDED);
                    var titleFont = new iTextSharp.text.Font(baseFont, 16, iTextSharp.text.Font.BOLD);
                    var objectFont = new iTextSharp.text.Font(baseFont, 11, iTextSharp.text.Font.BOLD);
                    var docFont = new iTextSharp.text.Font(baseFont, 10, iTextSharp.text.Font.NORMAL);

                    // --- Logo ---
                    if (System.IO.File.Exists(logoPath))
                    {
                        iTextSharp.text.Image logo = iTextSharp.text.Image.GetInstance(logoPath);

                        logo.ScaleAbsolute(90f, 90f);
                        logo.SetAbsolutePosition(36, doc.PageSize.Height - 18 - 100f);

                        doc.Add(logo);
                    }
                    // --- Tytuł ---
                    var title = new iTextSharp.text.Paragraph("Raport obiektów i dokumentów", titleFont)
                    {
                        Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                        SpacingBefore = 50f,
                        SpacingAfter = 30f
                    };
                    doc.Add(title);
                    // --- Główna treść raportu ---
                    foreach (var objectDB in objects)
                    {
                        var objectParagraph = new iTextSharp.text.Paragraph($"{objectDB.Name} (Lokalizacja: {objectDB.Location}, Model: {objectDB.Model})", objectFont);
                        doc.Add(objectParagraph);

                        var objectDocs = documents.Where(d => d.ObjectId == objectDB.Id).ToList();
                        if (objectDocs.Count == 0)
                        {
                            var noDocParagraph = new iTextSharp.text.Paragraph("- Brak dokumentów", docFont) { IndentationLeft = 20f };
                            doc.Add(noDocParagraph);
                        }
                        else
                        {
                            var docList = new iTextSharp.text.List(iTextSharp.text.List.UNORDERED, 10f);
                            docList.SetListSymbol(" - ");
                            docList.IndentationLeft = 20f;

                            foreach (var docItem in objectDocs)
                            {
                                string docInfo = $"{System.IO.Path.GetFileName(docItem.FilePath)} (dodano: {docItem.DateAdded:dd.MM.yyyy})";
                                docList.Add(new iTextSharp.text.ListItem(docInfo, docFont));
                            }
                            doc.Add(docList);
                        }
                        doc.Add(new iTextSharp.text.Paragraph(" ") { SpacingAfter = 5f });
                    }
                    doc.Close();
                }

                MessageBox.Show("Raport PDF został wygenerowany pomyślnie!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas generowania raportu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // PODGLĄD PDF
        private void DocumentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DocumentsDataGrid.SelectedItem is Document selectedDoc)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(selectedDoc.FilePath) { UseShellExecute = true });
                }
                catch
                {
                    MessageBox.Show("Nie można otworzyć pliku.");
                }
            }
        }
        // PANEL ADMINA
        private void AdminStatsButton_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new PanelStatystykAdmina();
            statsWindow.ShowDialog();
        }
        // USTAWIENIA
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
        // ------ HOOK DO WYŁĄCZENIA FULLSCREEN PRZY PRZECIĄGANIU -------
        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(lParam);
                handled = true;
            }

            return IntPtr.Zero;
        }
        private void WmGetMinMaxInfo(IntPtr lParam)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            var workArea = SystemParameters.WorkArea;

            mmi.ptMaxPosition.x = (int)workArea.Left;
            mmi.ptMaxPosition.y = (int)workArea.Top;
            mmi.ptMaxSize.x = (int)workArea.Width;
            mmi.ptMaxSize.y = (int)workArea.Height;

            Marshal.StructureToPtr(mmi, lParam, true);
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
        // -------------------------------------------------------------
        private void RefreshMiniLog()
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    string query = "SELECT UserName, Action, Target, Timestamp FROM ChangeLog ORDER BY Timestamp DESC LIMIT 1";
                    using (var cmd = new SQLiteCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string user = reader["UserName"]?.ToString() ?? "";
                            string action = reader["Action"]?.ToString() ?? "";
                            string target = reader["Target"]?.ToString() ?? "";

                            DateTime timestamp;
                            if (!DateTime.TryParse(reader["Timestamp"]?.ToString(), out timestamp))
                                timestamp = DateTime.Now;

                            string time = timestamp.ToString("HH:mm");
                            
                            MiniLogTextBlock.Text = $"{user}, godzina {time} - {action} ({target})";
                        }
                        else
                        {
                            MiniLogTextBlock.Text = "";
                        }
                    }
                }
            }
            catch
            {
                MiniLogTextBlock.Text = "";
            }
        }
        private void UpdateButtonStates()
        {
            bool isObjectSelected = ObjectsDataGrid.SelectedItem != null;
            var selectedDocument = DocumentsDataGrid.SelectedItem as Document; 
            bool isDocumentSelected = selectedDocument != null;
            bool selectedDocumentIsActive = isDocumentSelected && selectedDocument.IsActive;
            bool selectedDocumentHasHistory = false;

            if (isDocumentSelected && selectedDocument.VersionGroupId.HasValue)
            {
                selectedDocumentHasHistory = DatabaseHelper.DocumentHasHistory(selectedDocument.VersionGroupId.Value);
            }

            EditObjectButton.IsEnabled = isObjectSelected;
            DeleteObjectButton.IsEnabled = isObjectSelected;
            AddDocumentButton.IsEnabled = isObjectSelected;

            DeleteDocumentButton.IsEnabled = isDocumentSelected;
            ShowPdfButton.IsEnabled = isDocumentSelected;

            AddNewVersionButton.IsEnabled = selectedDocumentIsActive;
            ShowVersionHistoryButton.Visibility = selectedDocumentHasHistory ? Visibility.Visible : Visibility.Collapsed;
        }
        private void ClosePdfViewerButton_Click(object sender, RoutedEventArgs e)
        {
            PdfViewer.Visibility = Visibility.Collapsed;
            ClosePdfViewerButton.Visibility = Visibility.Collapsed;                                                                
        }
        private void ShowVersionHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentsDataGrid.SelectedItem is Document selectedDocument && selectedDocument.VersionGroupId.HasValue)
            {
                var historyWindow = new DocumentManagerApp.Views.VersionHistoryWindow(
                    selectedDocument.VersionGroupId.Value,
                    selectedDocument.FileName 
                );
                historyWindow.Owner = this;
                historyWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Nie można wyświetlić historii. Zaznaczony dokument nie ma grupy wersji lub nie jest zaznaczony.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void AddNewVersionButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentsDataGrid.SelectedItem is Document selectedDocument && selectedDocument.IsActive)
            {
                var addVersionWindow = new DocumentManagerApp.Views.AddDocumentWindow(selectedDocument);
                addVersionWindow.Owner = this;
                addVersionWindow.OnDataChanged = RefreshMiniLog;

                if (addVersionWindow.ShowDialog() == true)
                {
                    MessageBox.Show($"Nowa wersja (v{selectedDocument.Version + 1}) została dodana.");

                    if (ObjectsDataGrid.SelectedItem is Models.Object currentObject)
                    {
                        LoadDocumentsForObject(currentObject.Id);
                    }
                }
            }
            else
            {
                MessageBox.Show("Aby dodać nową wersję, zaznacz aktywny dokument.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
