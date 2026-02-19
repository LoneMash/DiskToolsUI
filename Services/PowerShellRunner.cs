// PowerShellRunner.cs - Version 2.2
// Changelog : Utilisation de AppContext.BaseDirectory pour résoudre les chemins relatifs
//             évite les erreurs quand l'app est lancée depuis un raccourci ou bat

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

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
            // Résolution du chemin relatif à partir du dossier de l'exe
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

                var scriptContent = File.ReadAllText(scriptPath);
                ps.AddScript(scriptContent);

                ps.Invoke();

                if (ps.HadErrors)
                {
                    var errors = string.Join(Environment.NewLine,
                        ps.Streams.Error.Select(e => e.ToString()));
                    _logger.LogError($"Erreurs lors du chargement du script '{scriptPath}': {errors}");
                    throw new Exception($"PowerShell script errors: {errors}");
                }

                _logger.LogInfo($"Script chargé avec succès : {scriptPath}");
            });
        }

        public async Task<Dictionary<string, string>> ExecuteFunctionAsync(
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
                    _logger.LogError($"Erreurs lors de l'exécution de '{functionName}': {errors}");
                    throw new Exception($"PowerShell execution errors: {errors}");
                }

                _logger.LogInfo($"Fonction '{functionName}' exécutée avec succès.");

                var resultDict = new Dictionary<string, string>();

                if (results.Count > 0)
                {
                    var firstResult = results[0];

                    if (firstResult.BaseObject is System.Collections.Hashtable hashtable)
                    {
                        foreach (var key in hashtable.Keys)
                            resultDict[key.ToString()!] = hashtable[key]?.ToString() ?? string.Empty;
                    }
                    else
                    {
                        foreach (var prop in firstResult.Properties)
                            resultDict[prop.Name] = prop.Value?.ToString() ?? string.Empty;
                    }
                }

                return resultDict;
            });
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
