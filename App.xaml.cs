// App.xaml.cs - Version 2.0
// Changelog : Routage au démarrage : mode silencieux (args CLI) ou mode UI normal
//             Mode silencieux : exécute la fonction PS et quitte avec code retour SCCM

using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using DiskToolsUi.Models;
using DiskToolsUi.Services;

namespace DiskToolsUi
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Handlers globaux d'exceptions
            DispatcherUnhandledException += (sender, args) =>
            {
                LogFatal("DispatcherUnhandledException", args.Exception);
                args.Handled = true;
                MessageBox.Show($"Erreur fatale UI :\n{args.Exception.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                LogFatal("UnhandledException", args.ExceptionObject as Exception);

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogFatal("UnobservedTaskException", args.Exception);
                args.SetObserved();
            };

            // Parser les arguments de ligne de commande
            var silentArgs = SilentArgs.Parse(e.Args);

            if (silentArgs.IsSilent)
            {
                // MODE SILENCIEUX : pas de fenêtre, exécution directe
                // Nécessite une console pour afficher les résultats
                AttachConsole();

                if (string.IsNullOrEmpty(silentArgs.Action))
                {
                    Console.Error.WriteLine("[ERREUR] --action est requis en mode silencieux.");
                    Console.Error.WriteLine("Usage : DiskToolsUi.exe --silent --action <FunctionName> [--ParamName Value] [--export <path.csv>]");
                    Console.Error.WriteLine("Exemple : DiskToolsUi.exe --silent --action Get-DiskInfo --DriveLetter C: --export C:\\log.csv");
                    Shutdown(1);
                    return;
                }

                var runner = new SilentRunner();
                int exitCode = await runner.RunAsync(silentArgs);
                Shutdown(exitCode);
            }
            else
            {
                // MODE UI NORMAL
                base.OnStartup(e);
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }

        /// <summary>
        /// Attache une console au processus WPF (OutputType=WinExe) 
        /// pour afficher les résultats en mode silencieux.
        /// </summary>
        private void AttachConsole()
        {
            // Si lancé depuis un terminal existant, s'y attache
            // Si lancé sans terminal (SCCM), crée une nouvelle console
            if (!AttachConsoleNative(-1))
                AllocConsole();
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private bool AttachConsoleNative(int pid)
        {
            return AttachConsole(pid);
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
