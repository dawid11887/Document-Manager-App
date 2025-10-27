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

            // Przykład: blokowanie przycisków dla zwykłego użytkownika
            if (!UserSession.IsAdmin)
            {
                AddObjectButton.Visibility = Visibility.Collapsed;
                DeleteObjectButton.Visibility = Visibility.Collapsed;
                EditObjectButton.Visibility = Visibility.Collapsed; 
                AddDocumentButton.Visibility = Visibility.Collapsed;
                DeleteDocumentButton.Visibility = Visibility.Collapsed; 
                AdminStatsButton.Visibility = Visibility.Collapsed;
                // itd.
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
            // jeśli chcesz, żeby MainWindow od razu wyglądał jak zmaksymalizowany (ale bez fullscreen)
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
        private Document selectedDocument = null; // albo inny typ danych reprezentujący dokument
        private void DocumentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDocument = DocumentsDataGrid.SelectedItem as Document;

            // ✨ UKRYJ podgląd i przycisk "X", jeśli dokument jest odznaczony ✨
            if (selectedDocument == null)
            {
                PdfViewer.Visibility = Visibility.Collapsed;
                ClosePdfViewerButton.Visibility = Visibility.Collapsed;
            }
            // Zawsze aktualizuj stan przycisków po zmianie zaznaczenia dokumentu
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
                    // ✨ POKAŻ podgląd i przycisk "X" ✨
                    PdfViewer.Visibility = Visibility.Visible;
                    ClosePdfViewerButton.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Plik PDF nie został znaleziony:\n" + pdfPath);
                    // ✨ UKRYJ podgląd i przycisk "X", jeśli plik nie istnieje ✨
                    PdfViewer.Visibility = Visibility.Collapsed;
                    ClosePdfViewerButton.Visibility = Visibility.Collapsed;
                }
            }
            // Nie musimy nic robić, jeśli nie wybrano dokumentu, bo przycisk jest nieaktywny
            // A jeśli był wcześniej widoczny, to zmiana zaznaczenia go ukryje (krok 3)
        }
        private Color GetRandomColor()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            byte r = (byte)rand.Next(50, 200);  // 50–200, żeby nie były zbyt jasne/ciemne
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

            // Wyczyść sesję
            UserSession.Logout();

            // Zapamiętaj referencję do aplikacji
            var app = Application.Current;

            // Ukryj stare okno od razu, żeby nie migało
            this.Hide();

            // Pokaż okno logowania
            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true && UserSession.CurrentUser != null)
            {
                // Stare okno już niepotrzebne – zamknij
                this.Close();

                // Uruchom nowy MainWindow po zalogowaniu
                var mainWindow = new MainWindow();
                app.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                // Zamyka aplikację jeśli anulowano logowanie
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
            addObjectWindow.Owner = this; // opcjonalnie, by okno było modalne względem MainWindow
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

            // Tworzymy okno, przekazując mu ZAZNACZONY obiekt
            var addDocWindow = new DocumentManagerApp.Views.AddDocumentWindow(selectedObject);
            addDocWindow.OnDataChanged = RefreshMiniLog;

            if (addDocWindow.ShowDialog() == true)
            {
                MessageBox.Show("Dokument dodany.");

                // Pobieramy ID maszyny z okna dialogowego
                int? objectId = addDocWindow.NewlyAddedDocumentObjectId;

                if (objectId.HasValue)
                {
                    // Sprawdzamy, czy tabela maszyn jest aktualnie zaznaczona na tej maszynie
                    if (ObjectsDataGrid.SelectedItem is Models.Object currentSelection && currentSelection.Id == objectId.Value)
                    {
                        // Jeśli tak, odświeżamy tylko listę dokumentów
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
                ApplyDocumentFilters(); // To ustawi ItemsSource dla DocumentsDataGrid
            }
            else
            {
                currentObjectDocuments.Clear();
                DocumentsDataGrid.ItemsSource = null;
                // ✨ UKRYJ podgląd i przycisk "X", gdy obiekt jest odznaczony ✨
                PdfViewer.Visibility = Visibility.Collapsed;
                ClosePdfViewerButton.Visibility = Visibility.Collapsed;
            }
            // Zawsze aktualizuj stan przycisków po zmianie zaznaczenia obiektu
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
            // jeśli którakolwiek kontrolka jest null, kończymy
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
            // Pracownik (Load) prosi Posłańca (Get) o dostarczenie danych
            var documents = GetDocumentsForObject(objectId);

            // Pracownik (Load) wykłada otrzymane dane na półkę (DataGrid)
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
                    // ✨ CAŁA LOGIKA BAZY DANYCH ZASTĄPIONA JEDNĄ LINIĄ ✨
                    DatabaseHelper.DeleteObjectAndAssociatedDocuments(selectedObject.Id);

                    DatabaseHelper.AddChangeLog(UserSession.CurrentUser.Username, "Usunięto obiekt", $"Obiekt: {selectedObject.Name}");
                    RefreshMiniLog();

                    // Ścieżka do folderu maszyny
                    string baseStoragePath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DocumentsStorage");
                    string objectFolder = System.IO.Path.Combine(baseStoragePath, $"Object_{selectedObject.Id}");

                    // Sprawdź, czy folder istnieje
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
                                    Directory.Delete(objectFolder, true); // usuwa również zawartość
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Błąd przy usuwaniu folderu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        else
                        {
                            // Folder pusty – usuń bez pytania
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
                // Ostrzeżenie (bez zmian)
                var result = MessageBox.Show($"Czy na pewno chcesz usunąć dokument '{selectedDoc.FileName}'\n***ORAZ WSZYSTKIE JEGO POPRZEDNIE WERSJE?***\n\nTej operacji nie będzie można cofnąć!",
                                              "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // --- 👇 NOWA LOGIKA USUWANIA PLIKÓW 👇 ---
                    int? groupId = selectedDoc.VersionGroupId;
                    List<string> filePathsToDelete = new List<string>();

                    // 1. Pobierz ścieżki wszystkich plików z grupy wersji
                    if (groupId.HasValue)
                    {
                        filePathsToDelete = DatabaseHelper.GetFilePathsForVersionGroup(groupId);
                    }
                    else
                    {
                        // Jeśli z jakiegoś powodu nie ma groupId, dodaj tylko bieżący plik
                        if (!string.IsNullOrEmpty(selectedDoc.FilePath))
                        {
                            filePathsToDelete.Add(selectedDoc.FilePath);
                        }
                    }

                    // 2. Usuń fizyczne pliki z dysku
                    string baseStoragePath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DocumentsStorage");
                    foreach (string relativePath in filePathsToDelete)
                    {
                        if (string.IsNullOrEmpty(relativePath)) continue; // Pomiń puste ścieżki

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
                                // Wyświetl błąd, ale kontynuuj usuwanie reszty i wpisów w bazie
                                MessageBox.Show($"Nie udało się usunąć pliku fizycznego:\n{fullFilePath}\nBłąd: {ex.Message}", "Błąd usuwania pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Plik nie znaleziony (pomijanie): {fullFilePath}");
                        }
                    }
                    // --- Koniec nowej logiki usuwania plików ---

                    // 3. Usuń wpisy z bazy danych (tak jak wcześniej)
                    DatabaseHelper.DeleteDocumentAndAllVersions(groupId); // Przekazujemy groupId

                    // Logika ChangeLog (bez zmian)
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

                    // Odśwież widok
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
            DocumentsDataGrid.ItemsSource = null; // czyszczenie dolnej tabeli
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
            // === Krok 1: Pobranie danych ===
            var objects = GetObjects();
            var documents = new List<ModelDocument>(); // Używamy Twojego aliasu 'ModelDocument'
            foreach (var objectDB in objects)
            {
                documents.AddRange(GetDocumentsForObject(objectDB.Id));
            }

            // === Krok 2: Zdefiniowanie ścieżek ===
            string datePart = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            string fileName = $"Raport_{datePart}.pdf";
            string reportsFolder = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Reports");
            System.IO.Directory.CreateDirectory(reportsFolder);
            string outputPath = System.IO.Path.Combine(reportsFolder, fileName);
            string logoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");

            // === Krok 3: Generowanie PDF za pomocą iTextSharp (z pełnymi nazwami klas) ===
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

                        // ZMIANA: Ustawiamy szerokość i wysokość na tę samą, większą wartość.
                        logo.ScaleAbsolute(90f, 90f);

                        // ZMIANA: Aktualizujemy pozycję Y, uwzględniając nową, większą wysokość logo.
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
                            
                            // Parsujemy datę i formatujemy tylko godzina:minuty
                            DateTime timestamp;
                            if (!DateTime.TryParse(reader["Timestamp"]?.ToString(), out timestamp))
                                timestamp = DateTime.Now;

                            string time = timestamp.ToString("HH:mm"); // godziny:minuty
                            
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
            var selectedDocument = DocumentsDataGrid.SelectedItem as Document; // Pobieramy zaznaczony dokument
            bool isDocumentSelected = selectedDocument != null;
            bool selectedDocumentIsActive = isDocumentSelected && selectedDocument.IsActive;
            bool selectedDocumentHasHistory = false;

            // Sprawdzamy historię tylko, jeśli dokument jest zaznaczony i ma VersionGroupId
            if (isDocumentSelected && selectedDocument.VersionGroupId.HasValue)
            {
                selectedDocumentHasHistory = DatabaseHelper.DocumentHasHistory(selectedDocument.VersionGroupId.Value);
            }

            // Ustawiamy IsEnabled/Visibility dla przycisków obiektów
            EditObjectButton.IsEnabled = isObjectSelected;
            DeleteObjectButton.IsEnabled = isObjectSelected;
            AddDocumentButton.IsEnabled = isObjectSelected; // Można dodać dokument (wersję 1) tylko do obiektu

            // Ustawiamy IsEnabled/Visibility dla przycisków dokumentów
            DeleteDocumentButton.IsEnabled = isDocumentSelected; // Można usunąć dowolną wersję (?) - do przemyślenia
            ShowPdfButton.IsEnabled = isDocumentSelected;

            // Logika dla nowych przycisków wersji
            AddNewVersionButton.IsEnabled = selectedDocumentIsActive; // Aktywny tylko dla aktywnej wersji
            ShowVersionHistoryButton.Visibility = selectedDocumentHasHistory ? Visibility.Visible : Visibility.Collapsed; // Widoczny tylko, gdy jest historia
        }
        private void ClosePdfViewerButton_Click(object sender, RoutedEventArgs e)
        {
            PdfViewer.Visibility = Visibility.Collapsed; // Ukryj podgląd
            ClosePdfViewerButton.Visibility = Visibility.Collapsed; // Ukryj przycisk "X"
                                                                    // Opcjonalnie: Wyzeruj Source, jeśli chcesz zwolnić zasoby od razu, ale ukrycie powinno wystarczyć
                                                                    // if (PdfViewer != null && PdfViewer.CoreWebView2 != null)
                                                                    // {
                                                                    //     PdfViewer.CoreWebView2.Navigate("about:blank");
                                                                    // }
        }
        private void ShowVersionHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Pobieramy zaznaczony DOKUMENT
            if (DocumentsDataGrid.SelectedItem is Document selectedDocument && selectedDocument.VersionGroupId.HasValue)
            {
                // Tworzymy i pokazujemy nowe okno, przekazując ID grupy wersji i nazwę pliku
                var historyWindow = new DocumentManagerApp.Views.VersionHistoryWindow(
                    selectedDocument.VersionGroupId.Value,
                    selectedDocument.FileName // Przekazujemy nazwę pliku dla tytułu okna
                );
                historyWindow.Owner = this; // Ustawiamy MainWindow jako właściciela
                historyWindow.ShowDialog(); // ShowDialog() blokuje MainWindow do czasu zamknięcia okna historii
            }
            else
            {
                MessageBox.Show("Nie można wyświetlić historii. Zaznaczony dokument nie ma grupy wersji lub nie jest zaznaczony.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void AddNewVersionButton_Click(object sender, RoutedEventArgs e)
        {
            // Pobieramy zaznaczony DOKUMENT (powinien być aktywny, bo przycisk jest włączony)
            if (DocumentsDataGrid.SelectedItem is Document selectedDocument && selectedDocument.IsActive)
            {
                // Otwieramy okno AddDocumentWindow, używając NOWEGO konstruktora
                var addVersionWindow = new DocumentManagerApp.Views.AddDocumentWindow(selectedDocument);
                addVersionWindow.Owner = this; // Ustawienie właściciela okna
                addVersionWindow.OnDataChanged = RefreshMiniLog; // Podpięcie odświeżania logu

                if (addVersionWindow.ShowDialog() == true)
                {
                    MessageBox.Show($"Nowa wersja (v{selectedDocument.Version + 1}) została dodana.");

                    // Odświeżamy listę dokumentów dla bieżącego obiektu
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
