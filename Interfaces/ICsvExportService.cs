// ═══════════════════════════════════════════════════════════════════
// ICsvExportService.cs — Contrat d'export des résultats en CSV
// ═══════════════════════════════════════════════════════════════════
// Rôle : Définit l'export d'une collection de ResultItem vers un
//        fichier CSV, utilisé après l'exécution d'une action.
// Couche : Interfaces
// Consommé par : CsvExportService, MainWindowViewModel (export)
// ═══════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using RunDeck.Models;

namespace RunDeck.Interfaces
{
    public interface ICsvExportService
    {
        void Export(IEnumerable<ResultItem> results, string filePath);
    }
}
