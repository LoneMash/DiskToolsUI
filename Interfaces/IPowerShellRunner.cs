// ═══════════════════════════════════════════════════════════════════
// IPowerShellRunner.cs — Contrat d'exécution de scripts PowerShell
// ═══════════════════════════════════════════════════════════════════
// Rôle : Définit l'exécution d'actions PowerShell, en mode batch
//        (résultats complets) ou streaming (objets reçus un à un).
// Couche : Interfaces
// Consommé par : PowerShellRunner, MainWindowViewModel (exécution)
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using RunDeck.Models;

namespace RunDeck.Interfaces
{
    public interface IPowerShellRunner : IDisposable
    {
        Task<List<ResultItem>> ExecuteActionAsync(
            ActionDefinition action,
            Dictionary<string, object> parameters);

        Task ExecuteActionStreamingAsync(
            ActionDefinition action,
            Dictionary<string, object> parameters,
            Action<PSObject> onObjectReceived,
            CancellationToken cancellationToken = default);
    }
}
