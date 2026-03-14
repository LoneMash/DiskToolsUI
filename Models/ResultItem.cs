// Version 1.1
// Changelog :
//   1.0 - Initial
//   1.1 - v3.00 : Ajout de la propriété RawText pour le mode OutputType.Log

using System.Collections.ObjectModel;

namespace DiskToolsUi.Models
{
    public class ResultItem
    {
        /// <summary>Libellé (mode KeyValue)</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Valeur (mode KeyValue)</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>True si cet item représente un tableau</summary>
        public bool IsTable { get; set; } = false;

        /// <summary>True si cet item représente du texte brut (mode Log)</summary>
        public bool IsLog { get; set; } = false;

        /// <summary>Texte brut pour le mode Log</summary>
        public string RawText { get; set; } = string.Empty;

        /// <summary>En-têtes colonnes (mode Table)</summary>
        public ObservableCollection<string> Columns { get; set; } = new();

        /// <summary>Lignes (mode Table)</summary>
        public ObservableCollection<TableRow> Rows { get; set; } = new();
    }
}
