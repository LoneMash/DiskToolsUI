// ═══════════════════════════════════════════════════════════════════
// LoggerService.cs — Journalisation centralisée sur fichier
// ═══════════════════════════════════════════════════════════════════
// Rôle : Écrit les messages d'information, d'erreur et d'exception
//        dans un fichier log avec horodatage et niveaux de sévérité.
// Couche : Services
// Consommé par : PowerShellRunner, MainWindowViewModel, SilentRunner, App
// ═══════════════════════════════════════════════════════════════════
// LoggerService.cs - Version 2.3
// Changelog : Ajout de LogError(string, Exception) pour compatibilité avec
//             MainWindowViewModel et SilentRunner
//             Toutes les surcharges disponibles : Log, LogInfo, LogError
//   2.3 - Standardisation des préfixes de log (emoji → texte) pour compatibilité universelle

using System;
using System.IO;
using System.Text;
using RunDeck.Helpers;
using RunDeck.Interfaces;

namespace RunDeck.Services
{
    public class LoggerService : ILoggerService
    {
        private readonly string _logFilePath;

        public LoggerService(string? logFileName = "error.log")
        {
            _logFilePath = PathHelper.ResolveRelativePath(logFileName ?? "error.log");
        }

        /// <summary>Logue un message d'information.</summary>
        public void LogInfo(string message)
            => WriteLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO]  {message}\n" +
                        new string('-', 80) + "\n");

        /// <summary>Logue un message d'erreur texte simple.</summary>
        public void LogError(string message)
            => WriteLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}\n" +
                        new string('-', 80) + "\n");

        /// <summary>Logue une exception seule avec sa stack trace.</summary>
        public void LogError(Exception ex)
            => WriteLog(BuildExceptionMessage(ex));

        /// <summary>Logue un message + une exception — usage : LogError("contexte", ex).</summary>
        public void LogError(string message, Exception ex)
            => WriteLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}\n" +
                        BuildExceptionMessage(ex));

        /// <summary>Logue un message texte simple (générique).</summary>
        public void Log(string message)
            => WriteLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [LOG]   {message}\n" +
                        new string('-', 80) + "\n");

        /// <summary>Logue une exception générique.</summary>
        public void Log(Exception ex)
            => WriteLog(BuildExceptionMessage(ex));

        private void WriteLog(string message)
        {
            try
            {
                // Ouverture/fermeture à chaque écriture → pas de handle permanent
                using var stream = new FileStream(_logFilePath, FileMode.Append,
                                                  FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                writer.Write(message);
            }
            catch
            {
                // Ne bloque pas l'app si le log est inaccessible
            }
        }

        private string BuildExceptionMessage(Exception ex, int level = 0)
        {
            var indent = new string(' ', level * 2);
            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [FATAL] EXCEPTION (niveau {level})\n" +
                      $"{indent}Type       : {ex.GetType().FullName}\n" +
                      $"{indent}Message    : {ex.Message}\n" +
                      $"{indent}StackTrace : {ex.StackTrace}\n";
            if (ex.InnerException != null)
                msg += $"{indent}--- INNER EXCEPTION ---\n" +
                       BuildExceptionMessage(ex.InnerException, level + 1);
            msg += new string('-', 80) + "\n";
            return msg;
        }
    }
}
