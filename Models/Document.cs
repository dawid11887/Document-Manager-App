using System;
using System.IO;

namespace DocumentManagerApp.Models
{
    public class Document
    {
        public int Id { get; set; }
        public int ObjectId { get; set; }
        public string FilePath { get; set; }
        public string DocumentType { get; set; }
        public DateTime DateAdded { get; set; }
        public string Category { get; set; } = "Inne";
        public int Version { get; set; } = 1;
        public bool IsActive { get; set; } = true;    
        public int? VersionGroupId { get; set; }

        public string FileName => System.IO.Path.GetFileName(this.FilePath);
    }
}

