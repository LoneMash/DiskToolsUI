// LoggerService.cs - Version 1.1
// Changelog : error.log créé dans le dossier de l'exécutable (AppContext.BaseDirectory)

using System;
using System.IO;
using System.Text;

namespace DiskToolsUi.Services
{
    public class LoggerService
    {
        private readonly string _logFilePath;

        public LoggerService(string? logFileName = "error.log")
        {
            // Toujours à côté de l'exe : bin/Debug/net8.0-windows/error.log
            _logFilePath = Path.Combine(AppContext.BaseDirectory, logFileName ?? "error.log");

            var dir = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        public void LogError(string message, Exception? ex = null)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ ERREUR");
            logEntry.AppendLine($"  Message    : {message}");
            if (ex != null)
            {
                logEntry.AppendLine($"  Exception  : {ex.Message}");
                logEntry.AppendLine($"  StackTrace : {ex.StackTrace}");
            }
            logEntry.AppendLine(new string('-', 80));

            File.AppendAllText(_logFilePath, logEntry.ToString(), Encoding.UTF8);
        }

        public void LogInfo(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✅ INFO : {message}{Environment.NewLine}";
            File.AppendAllText(_logFilePath, logEntry, Encoding.UTF8);
        }
    }
}
