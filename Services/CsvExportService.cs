// ═══════════════════════════════════════════════════════════════════
// CsvExportService.cs — Export des résultats au format CSV
// ═══════════════════════════════════════════════════════════════════
// Rôle : Sérialise les ResultItem (Table, KeyValue, Log) en fichier CSV
//        avec séparateur point-virgule et échappement des caractères spéciaux.
// Couche : Services
// Consommé par : MainWindowViewModel (export GUI), SilentRunner (export CLI)
// ═══════════════════════════════════════════════════════════════════
// CsvExportService.cs - Version 2.0
// Changelog :
//   1.1 - Ajout using System.Linq manquant pour .Select() sur ObservableCollection
//   2.0 - Support export depuis LiveTable (streaming temps réel)

using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using RunDeck.Interfaces;
using RunDeck.Models;

namespace RunDeck.Services
{
    public class CsvExportService : ICsvExportService
    {
        /// <summary>
        /// Exporte une liste de ResultItem vers un fichier CSV.
        /// Gère les deux modes : clé/valeur et tableau.
        /// </summary>
        public void Export(IEnumerable<ResultItem> results, string filePath)
        {
            var sb = new StringBuilder();

            foreach (var item in results)
            {
                switch (item)
                {
                    case TableResult table:
                        // MODE TABLEAU — priorise LiveTable (streaming) si disponible
                        if (table.LiveTable != null)
                        {
                            var colNames = table.LiveTable.Columns.Cast<DataColumn>()
                                .Select(c => c.ColumnName);
                            sb.AppendLine(string.Join(";", colNames));

                            foreach (DataRow dr in table.LiveTable.Rows)
                            {
                                var cells = dr.ItemArray.Select(c => EscapeCsv(c?.ToString() ?? ""));
                                sb.AppendLine(string.Join(";", cells));
                            }
                        }
                        else
                        {
                            sb.AppendLine(string.Join(";", table.Columns));

                            foreach (var row in table.Rows)
                            {
                                var cells = row.Cells.Select(c => EscapeCsv(c));
                                sb.AppendLine(string.Join(";", cells));
                            }
                        }
                        break;

                    case LogResult log:
                        // MODE LOG : texte brut tel quel
                        sb.AppendLine(EscapeCsv(log.RawText));
                        break;

                    case KeyValueResult kv:
                        // MODE CLÉ/VALEUR : deux colonnes "Propriété;Valeur"
                        sb.AppendLine("Propriété;Valeur");
                        sb.AppendLine($"{EscapeCsv(kv.Label)};{EscapeCsv(kv.Value)}");
                        break;
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Échappe une valeur CSV : entoure de guillemets si elle contient
        /// un point-virgule, un guillemet ou un retour à la ligne.
        /// </summary>
        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}
