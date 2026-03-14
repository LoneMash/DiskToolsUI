// CsvExportService.cs - Version 1.1
// Changelog : Ajout using System.Linq manquant pour .Select() sur ObservableCollection

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RunDeck.Models;

namespace RunDeck.Services
{
    public class CsvExportService
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
                if (item.IsTable)
                {
                    // MODE TABLEAU : en-têtes + lignes
                    sb.AppendLine(string.Join(";", item.Columns));

                    foreach (var row in item.Rows)
                    {
                        var cells = row.Cells.Select(c => EscapeCsv(c));
                        sb.AppendLine(string.Join(";", cells));
                    }
                }
                else
                {
                    // MODE CLÉ/VALEUR : deux colonnes "Propriété;Valeur"
                    sb.AppendLine("Propriété;Valeur");
                    sb.AppendLine($"{EscapeCsv(item.Label)};{EscapeCsv(item.Value)}");
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
