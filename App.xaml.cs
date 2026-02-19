// App.xaml.cs - Version 1.1
// Changelog : Ajout de handlers globaux pour capturer les exceptions non catchées

using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DiskToolsUi
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Exception sur le thread UI
            DispatcherUnhandledException += (sender, args) =>
            {
                LogFatal("DispatcherUnhandledException", args.Exception);
                args.Handled = true;
                MessageBox.Show($"Erreur fatale UI :\n{args.Exception.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // Exception sur un thread background (Task, async...)
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                LogFatal("UnhandledException", args.ExceptionObject as Exception);
            };

            // Exception dans une Task non awaited
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogFatal("UnobservedTaskException", args.Exception);
                args.SetObserved();
            };

            base.OnStartup(e);
        }

        private void LogFatal(string source, Exception? ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "error.log");
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 💥 FATAL ({source})\n" +
                        $"  Message    : {ex?.Message}\n" +
                        $"  StackTrace : {ex?.StackTrace}\n" +
                        $"{new string('-', 80)}\n";
            File.AppendAllText(logPath, entry);
        }
    }
}
