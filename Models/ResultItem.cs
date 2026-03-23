// ═══════════════════════════════════════════════════════════════════
// ResultItem.cs — Hiérarchie des types de résultats d'exécution
// ═══════════════════════════════════════════════════════════════════
// Rôle : Classe abstraite et sous-classes typées (KeyValueResult,
//        TableResult, LogResult) pour les résultats PowerShell.
//        Utilise le polymorphisme au lieu de booléens discriminants.
// Couche : Models
// Consommé par : ResultsView, MainWindowViewModel, PowerShellRunner
// ═══════════════════════════════════════════════════════════════════

// Version 4.0
// Changelog :
//   1.0 - Initial
//   1.1 - v3.00 : Ajout de la propriété RawText pour le mode Log
//   2.0 - Ajout de TableView (DataView) pour tri natif dans le DataGrid
//   3.0 - Ajout de LiveTable (DataTable) pour streaming temps réel dans le DataGrid
//   4.0 - Refactorisation en hiérarchie typée : KeyValueResult, TableResult, LogResult
//          Suppression des booléens IsTable/IsLog au profit du polymorphisme

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;

namespace RunDeck.Models
{
    /// <summary>Classe de base abstraite pour tous les types de résultats.</summary>
    public abstract class ResultItem
    {
    }

    /// <summary>Résultat clé/valeur (une propriété et sa valeur).</summary>
    public class KeyValueResult : ResultItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Résultat tabulaire avec colonnes et lignes.</summary>
    public class TableResult : ResultItem
    {
        /// <summary>En-têtes colonnes</summary>
        public ObservableCollection<string> Columns { get; set; } = new();

        /// <summary>Lignes (mode batch)</summary>
        public ObservableCollection<TableRow> Rows { get; set; } = new();

        /// <summary>
        /// DataTable alimenté en temps réel par le streaming PowerShell.
        /// Quand défini, TableView retourne sa DefaultView (mise à jour automatique).
        /// </summary>
        public DataTable? LiveTable { get; set; }

        /// <summary>
        /// DataView utilisé par le DataGrid pour le tri natif par colonne.
        /// Priorise LiveTable (streaming) si disponible, sinon construit depuis Columns/Rows.
        /// </summary>
        public DataView? TableView
        {
            get
            {
                // Mode streaming : DataView live, mis à jour automatiquement
                if (LiveTable != null) return LiveTable.DefaultView;

                // Mode batch (fallback) : construit depuis Columns/Rows
                if (Columns.Count == 0) return null;

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

    /// <summary>Résultat texte brut (log).</summary>
    public class LogResult : ResultItem, INotifyPropertyChanged
    {
        private string _rawText = string.Empty;

        /// <summary>Texte brut pour le mode Log (observable pour streaming)</summary>
        public string RawText
        {
            get => _rawText;
            set
            {
                if (_rawText == value) return;
                _rawText = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
