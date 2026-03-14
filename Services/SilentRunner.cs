// Version 3.0
// Changelog :
//   1.0 - Initial
//   2.0 - Adaptation V3.0 — Parameters dans chaque ActionConfig
//   3.0 - v3.00 : Utilise ConfigService.LoadActionsAsync() + ActionDefinition
//                 Utilise PowerShellRunner.ExecuteActionAsync()
//                 Suppression de la dépendance à config.PowerShell.ScriptPath global

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiskToolsUi.Models;

namespace DiskToolsUi.Services
{
    public class SilentRunner
    {
        private readonly ConfigService _configService;
        private readonly PowerShellRunner _psRunner;
        private readonly CsvExportService _csvExport;
        private readonly LoggerService _logger;

        public SilentRunner()
        {
            _logger       = new LoggerService();
            _configService = new ConfigService();
            _psRunner     = new PowerShellRunner(_logger);
            _csvExport    = new CsvExportService();
        }

        public async Task<int> RunAsync(SilentArgs args)
        {
            try
            {
                _logger.LogInfo($"[SILENT] Démarrage — Action: '{args.Action}'");

                // 1. Charger la config globale pour récupérer le chemin de actions.json
                var appConfig = await _configService.LoadConfigAsync();

                // 2. Charger les actions
                var actions = await _configService.LoadActionsAsync(appConfig.ActionsFile);

                // 3. Vérifier que l'action demandée existe
                var action = actions.FirstOrDefault(a =>
                    a.Id.Equals(args.Action, StringComparison.OrdinalIgnoreCase) ||
                    (a.FunctionName?.Equals(args.Action, StringComparison.OrdinalIgnoreCase) ?? false));

                if (action == null)
                {
                    _logger.LogError($"[SILENT] Action introuvable : '{args.Action}'");
                    Console.Error.WriteLine($"[ERREUR] Action '{args.Action}' introuvable.");
                    Console.Error.WriteLine($"Actions disponibles : {string.Join(", ", actions.Select(a => a.Id))}");
                    return 1;
                }

                // 4. Construire les paramètres
                // Priorité : args CLI > valeurs par défaut définies dans actions.json
                var parameters = new Dictionary<string, object>();

                foreach (var p in action.Parameters)
                    parameters[p.Name] = args.Parameters.TryGetValue(p.Name, out var val)
                                         ? val
                                         : p.DefaultValue;

                // Paramètres CLI supplémentaires non déclarés dans actions.json
                foreach (var p in args.Parameters)
                    if (!parameters.ContainsKey(p.Key))
                        parameters[p.Key] = p.Value;

                _logger.LogInfo($"[SILENT] Paramètres : " +
                    $"{string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");

                // 5. Exécuter
                var results = await _psRunner.ExecuteActionAsync(action, parameters);

                // 6. Afficher dans la console
                Console.WriteLine($"\n=== {action.Name} ===");
                PrintResults(results);

                // 7. Export CSV si demandé
                if (!string.IsNullOrEmpty(args.ExportPath))
                {
                    var dir = Path.GetDirectoryName(args.ExportPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    _csvExport.Export(results, args.ExportPath);
                    Console.WriteLine($"\n[OK] Export CSV : {args.ExportPath}");
                    _logger.LogInfo($"[SILENT] Export CSV : {args.ExportPath}");
                }

                _logger.LogInfo("[SILENT] Terminé avec succès.");
                return 0;
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

        private void PrintResults(List<ResultItem> results)
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
                    Console.WriteLine($"{item.Label,-30} : {item.Value}");
                }
            }
        }
    }
}
