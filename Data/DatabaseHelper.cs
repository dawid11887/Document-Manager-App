using DocumentManagerApp.Models;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentManagerApp.Data
{
    public static class DatabaseHelper
    {
        private static readonly string dbFile = "data.db";
        private static readonly string connectionString = $"Data Source={dbFile};Version=3;";
        public static void InitializeDatabase()
        {
            if (!File.Exists(dbFile))
            {
                SQLiteConnection.CreateFile(dbFile);
                Console.WriteLine("Utworzono plik bazy danych.");
            }
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string createObjectsTable = @"
                CREATE TABLE IF NOT EXISTS Objects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Location TEXT,
                    Description TEXT,
                    Producent TEXT,
                    Model TEXT
                )";

                string createDocumentsTable = @"
                CREATE TABLE IF NOT EXISTS Documents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ObjectId INTEGER,
                    FilePath TEXT,
                    DocumentType TEXT,
                    DateAdded DATETIME,
                    Category TEXT DEFAULT 'Inne',
                    Version INTEGER DEFAULT 1,         
                    IsActive INTEGER DEFAULT 1,       
                    VersionGroupId INTEGER,            
                    FOREIGN KEY (ObjectId) REFERENCES Objects(Id)
                )";

                string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    Salt TEXT,
                    Role TEXT NOT NULL
                )";

                string createChangeLogTable = @"
                CREATE TABLE IF NOT EXISTS ChangeLog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserName TEXT NOT NULL,
                    Action TEXT NOT NULL,
                    Target TEXT,
                    Timestamp TEXT NOT NULL
                )";

                using (var command = new SQLiteCommand(createObjectsTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SQLiteCommand(createDocumentsTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SQLiteCommand(createUsersTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SQLiteCommand(createChangeLogTable, connection))
                {
                    command.ExecuteNonQuery();
                }
                using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Users", connection))
                {
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    // W pliku Data/DatabaseHelper.cs, wewnątrz metody InitializeDatabase
                    if (count == 0)
                    {
                        using (var insert = new SQLiteCommand("INSERT INTO Users (Username, Password, Salt, Role) VALUES (@username, @password, @salt, @role)", connection))
                        {
                            var (hash, salt) = Helpers.PasswordHelper.HashPassword("admin123");
                            insert.Parameters.AddWithValue("@username", "admin");
                            insert.Parameters.AddWithValue("@password", hash);
                            insert.Parameters.AddWithValue("@salt", salt);
                            insert.Parameters.AddWithValue("@role", "admin");
                            insert.ExecuteNonQuery();
                        }

                        using (var insert = new SQLiteCommand("INSERT INTO Users (Username, Password, Salt, Role) VALUES (@username, @password, @salt, @role)", connection))
                        {
                            var (hash, salt) = Helpers.PasswordHelper.HashPassword("user123");
                            insert.Parameters.AddWithValue("@username", "user");
                            insert.Parameters.AddWithValue("@password", hash);
                            insert.Parameters.AddWithValue("@salt", salt);
                            insert.Parameters.AddWithValue("@role", "user");
                            insert.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(connectionString);
        }
        public static void AddChangeLog(string userName, string action, string target)
        {
            using var connection = GetConnection();
            connection.Open();
            string query = "INSERT INTO ChangeLog (UserName, Action, Target, Timestamp) VALUES (@user, @action, @target, @timestamp)";
            using var cmd = new SQLiteCommand(query, connection);
            cmd.Parameters.AddWithValue("@user", userName);
            cmd.Parameters.AddWithValue("@action", action);
            cmd.Parameters.AddWithValue("@target", target ?? "");
            cmd.Parameters.AddWithValue("@timestamp", DateTime.Now);
            cmd.ExecuteNonQuery();
        }
        public static List<Models.Object> GetAllObjects()
        {
            var objects = new List<Models.Object>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM Objects";
                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        objects.Add(new Models.Object
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Location = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Producent = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Model = reader.IsDBNull(5) ? "" : reader.GetString(5)
                        });
                    }
                }
            }
            return objects;
        }
        public static List<Document> GetDocumentsForObject(int objectId)
        {
            var documents = new List<Document>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM Documents WHERE ObjectId = @ObjectId AND IsActive = 1 ORDER BY DateAdded DESC";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ObjectId", objectId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            documents.Add(new Document
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                ObjectId = reader.GetInt32(reader.GetOrdinal("ObjectId")),
                                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                                DocumentType = reader.GetString(reader.GetOrdinal("DocumentType")),
                                DateAdded = reader.GetDateTime(reader.GetOrdinal("DateAdded")),
                                Category = reader.GetString(reader.GetOrdinal("Category")),
                                Version = reader.GetInt32(reader.GetOrdinal("Version")),
                                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
                                VersionGroupId = reader.IsDBNull(reader.GetOrdinal("VersionGroupId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("VersionGroupId"))
                            });
                        }
                    }
                }
            }
            return documents;
        }
        public static void DeleteObjectAndAssociatedDocuments(int objectId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                // usunięcie wszystkich dokumentów powiązanych z obiektem
                var deleteDocsCmd = new SQLiteCommand("DELETE FROM Documents WHERE ObjectId = @ObjectId", connection);
                deleteDocsCmd.Parameters.AddWithValue("@ObjectId", objectId);
                deleteDocsCmd.ExecuteNonQuery();

                // usunięcie obiektu
                var deleteObjectCmd = new SQLiteCommand("DELETE FROM Objects WHERE Id = @Id", connection);
                deleteObjectCmd.Parameters.AddWithValue("@Id", objectId);
                deleteObjectCmd.ExecuteNonQuery();
            }
        }
        public static List<string> GetFilePathsForVersionGroup(int? versionGroupId)
        {
            var filePaths = new List<string>();
            if (!versionGroupId.HasValue)
            {
                return filePaths;
            }
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT FilePath FROM Documents WHERE VersionGroupId = @GroupId";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GroupId", versionGroupId.Value);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string filePath = reader.GetString(reader.GetOrdinal("FilePath"));
                            if (!string.IsNullOrEmpty(filePath))
                            {
                                filePaths.Add(filePath);
                            }
                        }
                    }
                }
            }
            return filePaths;
        }
        public static void DeleteDocumentAndAllVersions(int? versionGroupId) 
        {
            if (!versionGroupId.HasValue)
            {
                Console.WriteLine("Próba usunięcia dokumentu bez VersionGroupId.");
                return;
            }

            using (var connection = GetConnection())
            {
                connection.Open();
                // usuwamy WSZYSTKIE dokumenty należące do tej samej grupy wersji
                var cmd = new SQLiteCommand("DELETE FROM Documents WHERE VersionGroupId = @GroupId", connection);
                cmd.Parameters.AddWithValue("@GroupId", versionGroupId.Value);
                int affectedRows = cmd.ExecuteNonQuery();
                Console.WriteLine($"Usunięto {affectedRows} wersji dokumentu z grupy {versionGroupId.Value}.");
            }
        }
        public static void AddObject(Models.Object objectDB)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "INSERT INTO Objects (Name, Location, Description, Producent, Model) VALUES (@Name, @Location, @Description, @Producent, @Model)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", objectDB.Name);
                    command.Parameters.AddWithValue("@Location", objectDB.Location);
                    command.Parameters.AddWithValue("@Description", objectDB.Description);
                    command.Parameters.AddWithValue("@Producent", objectDB.Producent);
                    command.Parameters.AddWithValue("@Model", objectDB.Model);
                    command.ExecuteNonQuery();
                }
            }
        }
        public static void UpdateObject(Models.Object objectDB)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE Objects SET Name = @Name, Location = @Location, Description = @Description, Producent = @Producent, Model = @Model WHERE Id = @Id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", objectDB.Id);
                    command.Parameters.AddWithValue("@Name", objectDB.Name);
                    command.Parameters.AddWithValue("@Location", objectDB.Location);
                    command.Parameters.AddWithValue("@Description", objectDB.Description);
                    command.Parameters.AddWithValue("@Producent", objectDB.Producent);
                    command.Parameters.AddWithValue("@Model", objectDB.Model);
                    command.ExecuteNonQuery();
                }
            }
        }
        public static void AddDocument(Document document)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string insertQuery = "INSERT INTO Documents (ObjectId, FilePath, DocumentType, DateAdded, Category) " +
                                       "VALUES (@ObjectId, @FilePath, @DocumentType, @DateAdded, @Category); " +
                                       "SELECT last_insert_rowid();"; 

                long newDocumentId;

                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@ObjectId", document.ObjectId);
                    command.Parameters.AddWithValue("@FilePath", document.FilePath);
                    command.Parameters.AddWithValue("@DocumentType", document.DocumentType);
                    command.Parameters.AddWithValue("@DateAdded", document.DateAdded);
                    command.Parameters.AddWithValue("@Category", document.Category);

                    newDocumentId = (long)command.ExecuteScalar();
                }
                string updateQuery = "UPDATE Documents SET VersionGroupId = @Id WHERE Id = @Id";
                using (var updateCmd = new SQLiteCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@Id", newDocumentId);
                    updateCmd.ExecuteNonQuery();
                }
            }
        }
        public static bool DocumentHasHistory(int versionGroupId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                
                string query = "SELECT COUNT(*) FROM Documents WHERE VersionGroupId = @GroupId AND IsActive = 0";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GroupId", versionGroupId);
                    var count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0; 
                }
            }
        }
        public static void AddNewDocumentVersion(Document newVersionData, int oldVersionId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {                     
                        string updateQuery = "UPDATE Documents SET IsActive = 0 WHERE Id = @OldId";
                        using (var updateCmd = new SQLiteCommand(updateQuery, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@OldId", oldVersionId);
                            updateCmd.ExecuteNonQuery();
                        }
                        string insertQuery = @"INSERT INTO Documents
                            (ObjectId, FilePath, DocumentType, DateAdded, Category, Version, IsActive, VersionGroupId)
                            VALUES
                            (@ObjectId, @FilePath, @DocumentType, @DateAdded, @Category, @Version, @IsActive, @VersionGroupId)";
                        using (var insertCmd = new SQLiteCommand(insertQuery, connection, transaction))
                        {
                            insertCmd.Parameters.AddWithValue("@ObjectId", newVersionData.ObjectId);
                            insertCmd.Parameters.AddWithValue("@FilePath", newVersionData.FilePath);
                            insertCmd.Parameters.AddWithValue("@DocumentType", newVersionData.DocumentType);
                            insertCmd.Parameters.AddWithValue("@DateAdded", newVersionData.DateAdded);
                            insertCmd.Parameters.AddWithValue("@Category", newVersionData.Category);
                            insertCmd.Parameters.AddWithValue("@Version", newVersionData.Version);
                            insertCmd.Parameters.AddWithValue("@IsActive", 1);
                            insertCmd.Parameters.AddWithValue("@VersionGroupId", newVersionData.VersionGroupId);
                            insertCmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // w razie błędu cofnij wszystkie zmiany
                        transaction.Rollback();
                        Console.WriteLine($"Błąd podczas dodawania nowej wersji dokumentu: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public static Models.Object GetObjectById(int objectId)
        {
            Models.Object obj = null;
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM Objects WHERE Id = @Id LIMIT 1";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", objectId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            obj = new Models.Object
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? "" : reader.GetString(reader.GetOrdinal("Location")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                                Producent = reader.IsDBNull(reader.GetOrdinal("Producent")) ? "" : reader.GetString(reader.GetOrdinal("Producent")),
                                Model = reader.IsDBNull(reader.GetOrdinal("Model")) ? "" : reader.GetString(reader.GetOrdinal("Model"))
                            };
                        }
                    }
                }
            }
            return obj; // Zwróci null, jeśli nie znaleziono obiektu
        }
        public static List<Document> GetDocumentVersionHistory(int versionGroupId)
        {
            var documents = new List<Document>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM Documents WHERE VersionGroupId = @GroupId ORDER BY Version DESC";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@GroupId", versionGroupId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            documents.Add(new Document
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                ObjectId = reader.GetInt32(reader.GetOrdinal("ObjectId")),
                                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                                DocumentType = reader.GetString(reader.GetOrdinal("DocumentType")),
                                DateAdded = reader.GetDateTime(reader.GetOrdinal("DateAdded")),
                                Category = reader.GetString(reader.GetOrdinal("Category")),
                                Version = reader.GetInt32(reader.GetOrdinal("Version")),
                                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
                                VersionGroupId = reader.IsDBNull(reader.GetOrdinal("VersionGroupId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("VersionGroupId"))
                            });
                        }
                    }
                }
            }
            return documents;
        }
    }
}


