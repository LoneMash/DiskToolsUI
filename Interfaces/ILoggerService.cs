// ═══════════════════════════════════════════════════════════════════
// ILoggerService.cs — Contrat de journalisation applicative
// ═══════════════════════════════════════════════════════════════════
// Rôle : Définit les opérations de logging (info, erreur, exception)
//        utilisées dans toute l'application pour tracer l'activité.
// Couche : Interfaces
// Consommé par : LoggerService, MainWindowViewModel, PowerShellRunner
// ═══════════════════════════════════════════════════════════════════

using System;

namespace RunDeck.Interfaces
{
    public interface ILoggerService
    {
        void LogInfo(string message);
        void LogError(string message);
        void LogError(Exception ex);
        void LogError(string message, Exception ex);
        void Log(string message);
        void Log(Exception ex);
    }
}
