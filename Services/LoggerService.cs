using System;
using System.IO;
using System.Text;

namespace DiskToolsUi.Services
{
    public class LoggerService
    {
        private readonly string _logFilePath;

        public LoggerService(string logFilePath = "./error.log")
        {
            _logFilePath = logFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
        }

        public void LogError(string message, Exception? ex = null)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERREUR");
            logEntry.AppendLine($"Message: {message}");
            if (ex != null)
            {
                logEntry.AppendLine($"Exception: {ex.Message}");
                logEntry.AppendLine($"StackTrace: {ex.StackTrace}");
            }
            logEntry.AppendLine(new string('-', 80));

            File.AppendAllText(_logFilePath, logEntry.ToString());
        }

        public void LogInfo(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}{Environment.NewLine}";
            File.AppendAllText(_logFilePath, logEntry);
        }
    }
}
