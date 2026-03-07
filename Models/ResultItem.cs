// ResultItem.cs - Version 2.0 (inchangé)
// Changelog : Aucun changement pour la V3.0

using System.Collections.ObjectModel;

namespace DiskToolsUi.Models
{
    public class ResultItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsTable { get; set; } = false;
        public ObservableCollection<TableRow> Rows { get; set; } = new();
        public ObservableCollection<string> Columns { get; set; } = new();
    }

    public class TableRow
    {
        public ObservableCollection<string> Cells { get; set; } = new();
    }
}
