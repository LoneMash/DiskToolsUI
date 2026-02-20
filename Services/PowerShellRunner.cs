// PowerShellRunner.cs - Version 2.3
// Changelog : Support des arrays, PSCustomObject, Hashtable, et types simples
//             Retourne un PSResult qui encapsule tous les cas possibles

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using DiskToolsUi.Models;

namespace DiskToolsUi.Services
{
    public class PowerShellRunner : IDisposable
    {
        private readonly Runspace _runspace;
        private readonly LoggerService _logger;
        private bool _disposed;

        public PowerShellRunner(LoggerService? logger = null)
        {
            _logger = logger ?? new LoggerService();

            var initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

            _runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            _runspace.Open();

            _logger.LogInfo("PowerShellRunner initialisé.");
        }

        public async Task LoadScriptAsync(string scriptPath)
        {
            if (!Path.IsPathRooted(scriptPath))
                scriptPath = Path.Combine(AppContext.BaseDirectory, scriptPath);

            if (!File.Exists(scriptPath))
            {
                var msg = $"PowerShell script not found: {scriptPath}";
                _logger.LogError(msg);
                throw new FileNotFoundException(msg);
            }

            await Task.Run(() =>
            {
                using var ps = PowerShell.Create();
                ps.Runspace = _runspace;
                ps.AddScript(File.ReadAllText(scriptPath));
                ps.Invoke();

                if (ps.HadErrors)
                {
                    var errors = string.Join(Environment.NewLine,
                        ps.Streams.Error.Select(e => e.ToString()));
                    _logger.LogError($"Erreurs chargement script '{scriptPath}': {errors}");
                    throw new Exception($"PowerShell script errors: {errors}");
                }

                _logger.LogInfo($"Script chargé avec succès : {scriptPath}");
            });
        }

        public async Task<List<ResultItem>> ExecuteFunctionAsync(
            string functionName,
            Dictionary<string, object> parameters)
        {
            return await Task.Run(() =>
            {
                using var ps = PowerShell.Create();
                ps.Runspace = _runspace;

                ps.AddCommand(functionName);
                foreach (var param in parameters)
                    ps.AddParameter(param.Key, param.Value);

                var results = ps.Invoke();

                if (ps.HadErrors)
                {
                    var errors = string.Join(Environment.NewLine,
                        ps.Streams.Error.Select(e => e.ToString()));
                    _logger.LogError($"Erreurs exécution '{functionName}': {errors}");
                    throw new Exception($"PowerShell execution errors: {errors}");
                }

                _logger.LogInfo($"Fonction '{functionName}' exécutée avec succès.");

                return ParseResults(results);
            });
        }

        private List<ResultItem> ParseResults(System.Collections.ObjectModel.Collection<PSObject> results)
        {
            var items = new List<ResultItem>();

            if (results == null || results.Count == 0)
                return items;

            // CAS 1 : Un seul résultat → Hashtable ou PSCustomObject → affichage clé/valeur
            if (results.Count == 1)
            {
                var single = results[0];

                // Hashtable : @{ Key = "Value" }
                if (single.BaseObject is Hashtable hashtable)
                {
                    foreach (DictionaryEntry entry in hashtable)
                    {
                        items.Add(new ResultItem
                        {
                            Label = entry.Key?.ToString() ?? string.Empty,
                            Value = entry.Value?.ToString() ?? string.Empty
                        });
                    }
                    return items;
                }

                // Array/List dans un seul objet → traiter comme collection
                if (single.BaseObject is IEnumerable enumerable && single.BaseObject is not string)
                {
                    return ParseCollection(enumerable.Cast<object>().ToList());
                }
            }

            // CAS 2 : Plusieurs résultats → tableau
            if (results.Count > 1)
            {
                return ParseCollection(results.Cast<object>().ToList());
            }

            // CAS 3 : PSCustomObject (une seule entrée avec propriétés)
            var first = results[0];
            var props = first.Properties.ToList();

            if (props.Count > 0)
            {
                foreach (var prop in props)
                {
                    items.Add(new ResultItem
                    {
                        Label = prop.Name,
                        Value = prop.Value?.ToString() ?? string.Empty
                    });
                }
                return items;
            }

            // CAS 4 : Valeur simple (string, int, etc.)
            items.Add(new ResultItem
            {
                Label = "Résultat",
                Value = first.ToString()
            });

            return items;
        }

        private List<ResultItem> ParseCollection(List<object> objects)
        {
            var items = new List<ResultItem>();
            if (objects.Count == 0) return items;

            // Déterminer les colonnes depuis le premier objet
            var columns = new System.Collections.ObjectModel.ObservableCollection<string>();
            var rows = new System.Collections.ObjectModel.ObservableCollection<TableRow>();

            var firstObj = objects[0] is PSObject ps0 ? ps0 : PSObject.AsPSObject(objects[0]);

            // Colonnes depuis les propriétés du premier objet
            foreach (var prop in firstObj.Properties)
                columns.Add(prop.Name);

            // Remplir les lignes
            foreach (var obj in objects)
            {
                var psObj = obj is PSObject pso ? pso : PSObject.AsPSObject(obj);
                var row = new TableRow();

                foreach (var col in columns)
                {
                    var prop = psObj.Properties[col];
                    row.Cells.Add(prop?.Value?.ToString() ?? string.Empty);
                }

                rows.Add(row);
            }

            // Un seul ResultItem de type tableau
            items.Add(new ResultItem
            {
                IsTable = true,
                Columns = columns,
                Rows = rows
            });

            return items;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _runspace?.Dispose();
                _logger.LogInfo("PowerShellRunner libéré (Dispose).");
                _disposed = true;
            }
        }
    }
}
