// SilentRunner.cs - Version 4.0
// Changelog :
//   1.0 - Initial
//   3.0 - v3.00 : Utilise ConfigService + ActionDefinition + PowerShellRunner
//   4.0 - Ajout --all, --list, --help, --format json
//         Export auto-nommé si --export est un dossier
//         Affichage console amélioré

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RunDeck.Models;

namespace RunDeck.Services
{
    public class SilentRunner
    {
        private readonly ConfigService _configService;
        private readonly PowerShellRunner _psRunner;
        private readonly CsvExportService _csvExport;
        private readonly LoggerService _logger;

        private static readonly JsonSerializerOptions _jsonWriteOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public SilentRunner()
        {
            _logger        = new LoggerService();
            _configService = new ConfigService();
            _psRunner      = new PowerShellRunner(_logger);
            _csvExport     = new CsvExportService();
        }

        public async Task<int> RunAsync(SilentArgs args)
        {
            try
            {
                // --help
                if (args.ShowHelp)
                {
                    PrintHelp();
                    return 0;
                }

                // Charger la config
                var appConfig = await _configService.LoadConfigAsync();
                var actions = await _configService.LoadActionsAsync(appConfig.ActionsFile);

                // --list
                if (args.ListActions)
                {
                    PrintActionList(actions);
                    return 0;
                }

                // --all
                if (args.RunAll)
                    return await RunAllActions(actions, args);

                // --action <id>
                if (!string.IsNullOrEmpty(args.Action))
                    return await RunSingleAction(actions, args);

                // Aucun argument reconnu
                Console.Error.WriteLine("[ERREUR] Aucune action spécifiée. Utilisez --help pour l'aide.");
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError("[SILENT] Erreur fatale", ex);
                Console.Error.WriteLine($"[ERREUR] {ex.Message}");
                return 1;
            }
            finally
            {
                _psRunner.Dispose();
            }
        }

        // -------------------------------------------------------------------
        // Exécution d'une seule action
        // -------------------------------------------------------------------
        private async Task<int> RunSingleAction(List<ActionDefinition> actions, SilentArgs args)
        {
            var action = FindAction(actions, args.Action);
            if (action == null)
            {
                PrintActionNotFound(args.Action, actions);
                return 1;
            }

            _logger.LogInfo($"[SILENT] Démarrage — Action: '{action.Id}'");

            var parameters = BuildParameters(action, args.Parameters);
            var results = await _psRunner.ExecuteActionAsync(action, parameters);

            Console.WriteLine($"\n=== {action.Name} ===");
            PrintResults(results);

            if (!string.IsNullOrEmpty(args.ExportPath))
                ExportResults(action.Id, results, args.ExportPath, args.Format);

            _logger.LogInfo("[SILENT] Terminé avec succès.");
            return 0;
        }

        // -------------------------------------------------------------------
        // Exécution de toutes les actions
        // -------------------------------------------------------------------
        private async Task<int> RunAllActions(List<ActionDefinition> actions, SilentArgs args)
        {
            _logger.LogInfo($"[SILENT] Démarrage --all ({actions.Count} actions)");

            var allResults = new Dictionary<string, List<ResultItem>>();
            int errors = 0;

            foreach (var action in actions)
            {
                try
                {
                    Console.WriteLine($"\n=== {action.Name} ===");
                    var parameters = BuildParameters(action, args.Parameters);
                    var results = await _psRunner.ExecuteActionAsync(action, parameters);

                    PrintResults(results);
                    allResults[action.Id] = results;

                    // Export individuel si --export est un dossier
                    if (!string.IsNullOrEmpty(args.ExportPath) && IsDirectory(args.ExportPath))
                        ExportResults(action.Id, results, args.ExportPath, args.Format);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERREUR] {action.Name} : {ex.Message}");
                    _logger.LogError($"[SILENT] Erreur action '{action.Id}'", ex);
                    errors++;
                }
            }

            // Export consolidé si --export est un fichier
            if (!string.IsNullOrEmpty(args.ExportPath) && !IsDirectory(args.ExportPath))
                ExportAllResults(allResults, args.ExportPath, args.Format);

            Console.WriteLine($"\n--- {actions.Count - errors}/{actions.Count} action(s) réussie(s) ---");
            _logger.LogInfo($"[SILENT] --all terminé : {actions.Count - errors}/{actions.Count} OK");
            return errors > 0 ? 1 : 0;
        }

        // -------------------------------------------------------------------
        // Export
        // -------------------------------------------------------------------
        private void ExportResults(string actionId, List<ResultItem> results, string exportPath, string format)
        {
            var filePath = IsDirectory(exportPath)
                ? Path.Combine(exportPath, $"{actionId}_{DateTime.Now:yyyyMMdd_HHmmss}.{format}")
                : exportPath;

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (format == "json")
                ExportJson(results, filePath);
            else
                _csvExport.Export(results, filePath);

            Console.WriteLine($"[OK] Export {format.ToUpper()} : {filePath}");
            _logger.LogInfo($"[SILENT] Export {format.ToUpper()} : {filePath}");
        }

        private void ExportAllResults(Dictionary<string, List<ResultItem>> allResults, string filePath, string format)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (format == "json")
            {
                var consolidated = new Dictionary<string, object>();
                foreach (var (actionId, results) in allResults)
                {
                    consolidated[actionId] = results.Select(ResultToDict).ToList();
                }
                var json = JsonSerializer.Serialize(consolidated, _jsonWriteOptions);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            else
            {
                // CSV consolidé : chaque action séparée par un header
                var sb = new StringBuilder();
                foreach (var (actionId, results) in allResults)
                {
                    sb.AppendLine($"# {actionId}");
                    foreach (var item in results)
                    {
                        if (item.IsTable)
                        {
                            sb.AppendLine(string.Join(";", item.Columns));
                            foreach (var row in item.Rows)
                                sb.AppendLine(string.Join(";", row.Cells));
                        }
                        else if (!item.IsLog)
                        {
                            sb.AppendLine($"{item.Label};{item.Value}");
                        }
                    }
                    sb.AppendLine();
                }
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }

            Console.WriteLine($"[OK] Export consolidé {format.ToUpper()} : {filePath}");
            _logger.LogInfo($"[SILENT] Export consolidé : {filePath}");
        }

        private static void ExportJson(List<ResultItem> results, string filePath)
        {
            var data = results.Select(ResultToDict).ToList();
            var json = JsonSerializer.Serialize(data, _jsonWriteOptions);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        private static object ResultToDict(ResultItem item)
        {
            if (item.IsTable)
            {
                return new
                {
                    type = "table",
                    columns = item.Columns.ToList(),
                    rows = item.Rows.Select(r => r.Cells.ToList()).ToList()
                };
            }
            if (item.IsLog)
            {
                return new { type = "log", text = item.RawText };
            }
            return new { label = item.Label, value = item.Value };
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------
        private static ActionDefinition? FindAction(List<ActionDefinition> actions, string query)
        {
            return actions.FirstOrDefault(a =>
                a.Id.Equals(query, StringComparison.OrdinalIgnoreCase) ||
                (a.FunctionName?.Equals(query, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        private static Dictionary<string, object> BuildParameters(ActionDefinition action, Dictionary<string, string> cliParams)
        {
            var parameters = new Dictionary<string, object>();

            foreach (var p in action.Parameters)
                parameters[p.Name] = cliParams.TryGetValue(p.Name, out var val)
                                     ? val
                                     : p.DefaultValue;

            // Paramètres CLI supplémentaires non déclarés dans actions.json
            foreach (var p in cliParams)
                if (!parameters.ContainsKey(p.Key))
                    parameters[p.Key] = p.Value;

            return parameters;
        }

        private static bool IsDirectory(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString())
                || path.EndsWith(Path.AltDirectorySeparatorChar.ToString())
                || Directory.Exists(path);
        }

        // -------------------------------------------------------------------
        // Affichage console
        // -------------------------------------------------------------------
        private static void PrintResults(List<ResultItem> results)
        {
            foreach (var item in results)
            {
                if (item.IsTable)
                {
                    Console.WriteLine(string.Join(" | ", item.Columns));
                    Console.WriteLine(new string('-', 80));
                    foreach (var row in item.Rows)
                        Console.WriteLine(string.Join(" | ", row.Cells));
                }
                else if (item.IsLog)
                {
                    Console.WriteLine(item.RawText);
                }
                else
                {
                    Console.WriteLine($"  {item.Label,-30} : {item.Value}");
                }
            }
        }

        private static void PrintActionNotFound(string query, List<ActionDefinition> actions)
        {
            Console.Error.WriteLine($"[ERREUR] Action '{query}' introuvable.");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Actions disponibles :");
            foreach (var a in actions)
                Console.Error.WriteLine($"  {a.Id,-20} {a.Name}");
        }

        private static void PrintActionList(List<ActionDefinition> actions)
        {
            Console.WriteLine();
            Console.WriteLine("RunDeck — Actions disponibles");
            Console.WriteLine(new string('=', 60));

            string currentCategory = "";
            foreach (var a in actions)
            {
                if (a.Category != currentCategory)
                {
                    currentCategory = a.Category;
                    Console.WriteLine($"\n  [{currentCategory}]");
                }

                var paramInfo = a.Parameters.Count > 0
                    ? string.Join(", ", a.Parameters.Select(p =>
                        $"--{p.Name}{(p.Required ? " *" : "")}"))
                    : "(aucun paramètre)";

                Console.WriteLine($"    {a.Id,-20} {a.Name}");
                Console.WriteLine($"    {"",20} {paramInfo}");
            }

            Console.WriteLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine(@"
RunDeck — Mode silencieux (CLI)
===============================

USAGE :
  RunDeck.exe --silent --action <id> [--Param valeur] [--export chemin] [--format csv|json]
  RunDeck.exe --silent --all [--export chemin_ou_dossier] [--format csv|json]
  RunDeck.exe --list
  RunDeck.exe --help

OPTIONS :
  --silent, -s          Exécuter sans interface graphique
  --action <id>         Identifiant de l'action à exécuter (voir --list)
  --all, -a             Exécuter toutes les actions
  --export, -e <path>   Chemin d'export (fichier ou dossier pour --all)
  --format, -f <type>   Format d'export : csv (défaut) ou json
  --list, -l            Lister les actions disponibles avec leurs paramètres
  --help, -h            Afficher cette aide
  --<Param> <valeur>    Paramètre dynamique passé à la fonction PowerShell

EXEMPLES :
  RunDeck.exe --list
  RunDeck.exe -s --action disk-info --DriveLetter C
  RunDeck.exe -s --action system-info -e rapport.json -f json
  RunDeck.exe -s --all -e C:\Rapports\ -f csv
  RunDeck.exe -s --all -e C:\rapport_complet.json -f json

NOTES :
  - Si --export est un dossier (avec --all), un fichier par action est créé
  - Si --export est un fichier (avec --all), toutes les données sont consolidées
  - Les paramètres CLI sont prioritaires sur les valeurs par défaut de actions.json
");
        }
    }
}
