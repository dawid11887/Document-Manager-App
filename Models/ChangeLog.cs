using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentManagerApp.Models
{
    public class ChangeLog
    {
        public int Id { get; set; }                // klucz główny
        public string UserName { get; set; }       // kto wykonał akcję
        public string Action { get; set; }         // co zrobił
        public string Target { get; set; }         // czego dotyczy (np. "OBIEKT: XYZ")
        public DateTime Timestamp { get; set; }    // kiedy
    }
}