// ═══════════════════════════════════════════════════════════════════
// TableRow.cs — Ligne de données pour les résultats tabulaires
// ═══════════════════════════════════════════════════════════════════
// Rôle : Conteneur simple d'une collection de cellules (strings)
//        utilisé par TableResult en mode batch.
// Couche : Models
// Consommé par : TableResult, PowerShellRunner
// ═══════════════════════════════════════════════════════════════════

// Version 1.0
// Changelog :
//   1.0 - Initial — inchangé en v3.00

using System.Collections.ObjectModel;

namespace RunDeck.Models
{
    public class TableRow
    {
        public ObservableCollection<string> Cells { get; set; } = new();
    }
}
