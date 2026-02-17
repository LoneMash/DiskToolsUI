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
        private bool _disposed;

        public PowerShellRunner()
        {
            var initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;
            
            _runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            _runspace.Open();
        }

        public async Task LoadScriptAsync(string scriptPath)
        {
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"PowerShell script not found: {scriptPath}");
            }

            await Task.Run(() =>
            {
                using var ps = PowerShell.Create();
                ps.Runspace = _runspace;
                
                var scriptContent = File.ReadAllText(scriptPath);
                ps.AddScript(scriptContent);
                
                var results = ps.Invoke();
                
                if (ps.HadErrors)
                {
                    var errors = string.Join(Environment.NewLine, 
                        ps.Streams.Error.Select(e => e.ToString()));
                    throw new Exception($"PowerShell script errors: {errors}");
                }
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
                {
                    ps.AddParameter(param.Key, param.Value);
                }
                
                var results = ps.Invoke();
                
                if (ps.HadErrors)
                {
                    var errors = string.Join(Environment.NewLine, 
                        ps.Streams.Error.Select(e => e.ToString()));
                    throw new Exception($"PowerShell execution errors: {errors}");
                }

                var resultDict = new Dictionary<string, string>();
                
                if (results.Count > 0)
                {
                    var firstResult = results[0];
                    
                    if (firstResult.BaseObject is System.Collections.Hashtable hashtable)
                    {
                        foreach (var key in hashtable.Keys)
                        {
                            resultDict[key.ToString()!] = hashtable[key]?.ToString() ?? string.Empty;
                        }
                    }
                    else
                    {
                        foreach (var prop in firstResult.Properties)
                        {
                            resultDict[prop.Name] = prop.Value?.ToString() ?? string.Empty;
                        }
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
                _disposed = true;
            }
        }
    }
}
