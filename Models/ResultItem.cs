// ResultItem.cs - Version 2.0
// Changelog : Ajout IsTableRow et Columns pour supporter l'affichage en tableau
//             (arrays et collections retournés par PowerShell)

using System.Collections.ObjectModel;

namespace DiskToolsUi.Models
{
    public class ResultItem
    {
        // Mode clé/valeur simple (Hashtable, PSCustomObject)
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        // Mode tableau (Array, List)
        public bool IsTable { get; set; } = false;
        public ObservableCollection<TableRow> Rows { get; set; } = new();
        public ObservableCollection<string> Columns { get; set; } = new();
    }

    public class TableRow
    {
        public ObservableCollection<string> Cells { get; set; } = new();
    }
}
