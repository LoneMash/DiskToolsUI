// ═══════════════════════════════════════════════════════════════════
// IResultBuilder.cs — Contrat de transformation PSObject → ResultItem
// ═══════════════════════════════════════════════════════════════════
// Rôle : Convertit les objets PowerShell (PSObject) en résultats
//        typés (KeyValue, Table, Log) et gère l'état de streaming.
//        Inclut aussi la classe StreamingContext (état partagé).
// Couche : Interfaces
// Consommé par : ResultBuilder, PowerShellRunner, MainWindowViewModel
// ═══════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using System.Management.Automation;
using RunDeck.Models;

namespace RunDeck.Interfaces
{
    /// <summary>
    /// Transforme les PSObject en ResultItem typés.
    /// Gère l'état de streaming (détection du type, accumulation des résultats).
    /// </summary>
    public interface IResultBuilder
    {
        /// <summary>Crée un nouvel état de streaming pour une action.</summary>
        StreamingContext CreateContext();

        /// <summary>
        /// Traite un PSObject reçu en streaming.
        /// Retourne les nouveaux ResultItem à ajouter à la collection (peut être vide).
        /// Peut aussi mettre à jour un résultat existant (ajout de ligne table, append log).
        /// </summary>
        List<ResultItem> ProcessStreamingObject(PSObject obj, StreamingContext context);

        /// <summary>Convertit un PSObject en une liste de KeyValueResult.</summary>
        List<KeyValueResult> BuildKeyValues(PSObject obj);
    }

    /// <summary>État partagé pendant le streaming d'une action.</summary>
    public class StreamingContext
    {
        public bool TypeDetected;
        public string DetectedType = string.Empty;
        public TableResult? CurrentTable;
        public LogResult? CurrentLog;
        public List<string>? TableColumns;
        public int ObjectCount;
    }
}
