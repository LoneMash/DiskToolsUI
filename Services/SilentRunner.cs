// SilentRunner.cs - Version 2.0
// Changelog : Adaptation V3.0 — Parameters déplacés dans chaque ActionConfig
//             Construction des paramètres depuis action.Parameters au lieu de config.UI.Parameters

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
            _logger        = new LoggerService();
            _configService = new ConfigService();
            _psRunner      = new PowerShellRunner(_logger);
            _csvExport     = new CsvExportService();
        }

        public async Task<int> RunAsync(SilentArgs args)
        {
            try
            {
                _logger.LogInfo($"[SILENT] Démarrage -- Action: '{args.Action}'");

                // 1. Charger la config
                var config = await _configService.LoadConfigAsync();

                // 2. Vérifier que l'action existe dans config.json
                var actionConfig = config.UI.Actions
                    .FirstOrDefault(a => a.FunctionName.Equals(
                        args.Action, StringComparison.OrdinalIgnoreCase));

                if (actionConfig == null)
                {
                    _logger.LogError($"[SILENT] Action introuvable : '{args.Action}'");
                    Console.Error.WriteLine($"[ERREUR] Action '{args.Action}' introuvable dans config.json.");
                    Console.Error.WriteLine($"Actions disponibles : {string.Join(", ", config.UI.Actions.Select(a => a.FunctionName))}");
                    return 1;
                }

                // 3. Charger le script PowerShell
                await _psRunner.LoadScriptAsync(config.PowerShell.ScriptPath);

                // 4. Construire les paramètres
                // Priorité : args CLI > valeurs par défaut de l'action dans config.json
                var parameters = new Dictionary<string, object>();

                // Valeurs par défaut depuis les Parameters de l'action
                foreach (var p in actionConfig.Parameters)
                {
                    parameters[p.Name] = args.Parameters.ContainsKey(p.Name)
                        ? args.Parameters[p.Name]
                        : p.Default;
                }

                // Paramètres CLI supplémentaires non déclarés dans config.json
                foreach (var p in args.Parameters)
                {
                    if (!parameters.ContainsKey(p.Key))
                        parameters[p.Key] = p.Value;
                }

                _logger.LogInfo($"[SILENT] Paramètres : {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");

                // 5. Exécuter la fonction PowerShell
                var results = await _psRunner.ExecuteFunctionAsync(actionConfig.FunctionName, parameters);

                // 6. Afficher les résultats dans la console
                Console.WriteLine($"\n=== {actionConfig.Name} ===");
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
                else
                {
                    Console.WriteLine($"{item.Label,-30} : {item.Value}");
                }
            }
        }
    }
}
