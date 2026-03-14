// Version 4.0
// Changelog :
//   1.0 - Initial
//   2.3 - Support arrays, PSCustomObject, Hashtable, types simples
//   3.0 - v3.00 : Suppression de LoadScriptAsync() unique global
//                 Ajout de ExecuteActionAsync(ActionDefinition, Dictionary<string,object>)
//                 Support script autonome (FunctionName null) + script avec fonction
//                 Cache des scripts déjà chargés (évite les rechargements inutiles)
//                 Runspace recréé proprement si script autonome (isolation)
//   4.0 - Auto-détection du type de sortie (Table, KeyValue, Log)
//          Suppression de la dépendance à OutputType dans ActionDefinition

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using DiskToolsUi.Helpers;
using DiskToolsUi.Models;

namespace DiskToolsUi.Services
{
    public class PowerShellRunner : IDisposable
    {
        private readonly LoggerService _logger;
        private readonly HashSet<string> _loadedScripts = new(StringComparer.OrdinalIgnoreCase);
        private Runspace _runspace;
        private bool _disposed;

        public PowerShellRunner(LoggerService? logger = null)
        {
            _logger = logger ?? new LoggerService();
            _runspace = CreateRunspace();
            _logger.LogInfo("PowerShellRunner initialisé.");
        }

        // -----------------------------------------------------------------------
        // Point d'entrée principal — remplace LoadScriptAsync + ExecuteFunctionAsync
        // -----------------------------------------------------------------------
        public async Task<List<ResultItem>> ExecuteActionAsync(
            ActionDefinition action,
            Dictionary<string, object> parameters)
        {
            return await Task.Run(() =>
            {
                var scriptPath = PathHelper.ResolveRelativePath(action.ScriptPath);

                if (!File.Exists(scriptPath))
                {
                    var msg = $"Script introuvable : {scriptPath}";
                    _logger.LogError(msg);
                    throw new FileNotFoundException(msg);
                }

                // Script autonome → runspace isolé, pas de cache
                if (action.FunctionName == null)
                    return ExecuteStandaloneScript(scriptPath, parameters);

                // Script avec fonction → chargement avec cache
                LoadScriptIfNeeded(scriptPath);
                return ExecuteFunction(action.FunctionName, parameters);
            });
        }

        // -----------------------------------------------------------------------
        // Exécution script autonome (FunctionName null)
        // Runspace isolé pour éviter les collisions entre scripts
        // -----------------------------------------------------------------------
        private List<ResultItem> ExecuteStandaloneScript(
            string scriptPath,
            Dictionary<string, object> parameters)
        {
            using var isolatedRunspace = CreateRunspace();
            using var ps = PowerShell.Create();
            ps.Runspace = isolatedRunspace;

            var scriptContent = File.ReadAllText(scriptPath);
            ps.AddScript(scriptContent);

            foreach (var param in parameters)
                ps.AddParameter(param.Key, param.Value);

            var results = ps.Invoke();
            CheckErrors(ps, scriptPath);

            _logger.LogInfo($"Script autonome exécuté : {scriptPath}");
            return ParseResults(results);
        }

        // -----------------------------------------------------------------------
        // Chargement script dans le runspace partagé (avec cache)
        // -----------------------------------------------------------------------
        private void LoadScriptIfNeeded(string scriptPath)
        {
            if (_loadedScripts.Contains(scriptPath))
            {
                _logger.LogInfo($"Script déjà en cache : {scriptPath}");
                return;
            }

            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddScript(File.ReadAllText(scriptPath));
            ps.Invoke();

            CheckErrors(ps, scriptPath);

            _loadedScripts.Add(scriptPath);
            _logger.LogInfo($"Script chargé dans le runspace : {scriptPath}");
        }

        // -----------------------------------------------------------------------
        // Exécution d'une fonction dans le runspace partagé
        // -----------------------------------------------------------------------
        private List<ResultItem> ExecuteFunction(
            string functionName,
            Dictionary<string, object> parameters)
        {
            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;

            ps.AddCommand(functionName);
            foreach (var param in parameters)
                ps.AddParameter(param.Key, param.Value);

            var results = ps.Invoke();
            CheckErrors(ps, functionName);

            _logger.LogInfo($"Fonction '{functionName}' exécutée avec succès.");
            return ParseResults(results);
        }

        // -----------------------------------------------------------------------
        // Vérification des erreurs PowerShell
        // -----------------------------------------------------------------------
        private void CheckErrors(PowerShell ps, string context)
        {
            if (!ps.HadErrors) return;

            var errors = string.Join(Environment.NewLine,
                ps.Streams.Error.Select(e => e.ToString()));
            _logger.LogError($"Erreurs PS ({context}) : {errors}");
            throw new Exception($"Erreurs PowerShell [{context}] :\n{errors}");
        }

        // -----------------------------------------------------------------------
        // Auto-détection du type de sortie et routage du parsing
        //   - Hashtable ou objet unique     → KeyValue
        //   - Plusieurs PSCustomObject       → Table
        //   - Chaînes de texte              → Log
        // -----------------------------------------------------------------------
        private List<ResultItem> ParseResults(Collection<PSObject> results)
        {
            if (results == null || results.Count == 0)
            {
                _logger.LogInfo("Aucun résultat retourné par PowerShell.");
                return new List<ResultItem>();
            }

            var first = results[0].BaseObject;

            // Hashtable → toujours KeyValue
            if (first is Hashtable)
            {
                _logger.LogInfo("Type détecté : KeyValue (Hashtable)");
                return ParseKeyValue(results);
            }

            // Chaîne de texte → Log
            if (first is string)
            {
                _logger.LogInfo("Type détecté : Log (String)");
                return ParseLog(results);
            }

            // Plusieurs objets avec propriétés → Table
            if (results.Count > 1 && results[0].Properties.Any())
            {
                _logger.LogInfo("Type détecté : Table (multiple objects)");
                return ParseTable(results);
            }

            // Objet unique avec propriétés → KeyValue
            _logger.LogInfo("Type détecté : KeyValue (single object)");
            return ParseKeyValue(results);
        }

        // -----------------------------------------------------------------------
        // Mode KeyValue
        // -----------------------------------------------------------------------
        private static List<ResultItem> ParseKeyValue(Collection<PSObject> results)
        {
            var items = new List<ResultItem>();

            foreach (var obj in results)
            {
                // Hashtable : @{ Key = "Value" }
                if (obj.BaseObject is Hashtable hashtable)
                {
                    foreach (DictionaryEntry entry in hashtable)
                        items.Add(new ResultItem
                        {
                            Label = entry.Key?.ToString() ?? string.Empty,
                            Value = entry.Value?.ToString() ?? string.Empty
                        });
                    continue;
                }

                // PSCustomObject ou objet typé → propriétés
                var props = obj.Properties.ToList();
                if (props.Count > 0)
                {
                    foreach (var prop in props)
                        items.Add(new ResultItem
                        {
                            Label = prop.Name,
                            Value = prop.Value?.ToString() ?? string.Empty
                        });
                    continue;
                }

                // Valeur simple (string, int…)
                items.Add(new ResultItem
                {
                    Label = "Résultat",
                    Value = obj.ToString()
                });
            }

            return items;
        }

        // -----------------------------------------------------------------------
        // Mode Table
        // -----------------------------------------------------------------------
        private static List<ResultItem> ParseTable(Collection<PSObject> results)
        {
            var columns = new ObservableCollection<string>();
            var rows    = new ObservableCollection<TableRow>();

            // Colonnes depuis le premier objet
            var firstObj = results[0];
            foreach (var prop in firstObj.Properties)
                columns.Add(prop.Name);

            // Lignes
            foreach (var obj in results)
            {
                var row = new TableRow();
                foreach (var col in columns)
                {
                    var prop = obj.Properties[col];
                    row.Cells.Add(prop?.Value?.ToString() ?? string.Empty);
                }
                rows.Add(row);
            }

            return new List<ResultItem>
            {
                new ResultItem
                {
                    IsTable = true,
                    Columns = columns,
                    Rows    = rows
                }
            };
        }

        // -----------------------------------------------------------------------
        // Mode Log — texte brut, une ligne par objet retourné
        // -----------------------------------------------------------------------
        private static List<ResultItem> ParseLog(Collection<PSObject> results)
        {
            var lines = results
                .Select(r => r?.ToString() ?? string.Empty)
                .Where(l => !string.IsNullOrEmpty(l));

            return new List<ResultItem>
            {
                new ResultItem
                {
                    IsLog   = true,
                    RawText = string.Join(Environment.NewLine, lines)
                }
            };
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        private static Runspace CreateRunspace()
        {
            var iss = InitialSessionState.CreateDefault();
            iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;
            var rs = RunspaceFactory.CreateRunspace(iss);
            rs.Open();
            return rs;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _runspace?.Dispose();
            _logger.LogInfo("PowerShellRunner libéré (Dispose).");
            _disposed = true;
        }
    }
}
