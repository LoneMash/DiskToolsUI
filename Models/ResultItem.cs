// Version 2.0
// Changelog :
//   1.0 - Initial
//   1.1 - v3.00 : Ajout de la propriété RawText pour le mode Log
//   2.0 - Ajout de TableView (DataView) pour tri natif dans le DataGrid

using System.Collections.ObjectModel;
using System.Data;

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

        /// <summary>
        /// DataView construit depuis Columns/Rows — utilisé par le DataGrid
        /// pour le tri natif par colonne.
        /// </summary>
        public DataView? TableView
        {
            get
            {
                if (!IsTable || Columns.Count == 0) return null;

                var dt = new DataTable();
                foreach (var col in Columns)
                    dt.Columns.Add(col, typeof(string));

                foreach (var row in Rows)
                {
                    var dr = dt.NewRow();
                    for (int i = 0; i < row.Cells.Count && i < Columns.Count; i++)
                        dr[i] = row.Cells[i];
                    dt.Rows.Add(dr);
                }

                return dt.DefaultView;
            }
        }
    }
}
