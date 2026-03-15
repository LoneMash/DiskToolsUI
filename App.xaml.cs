// App.xaml.cs - Version 3.0
// Changelog :
//   2.3 - Capture des exceptions non gérées via LoggerService
//   3.0 - Branchement du mode silencieux (CLI) via SilentArgs
//         --silent, --all, --list, --help interceptés au démarrage

using System;
using System.Windows;
using RunDeck.Models;
using RunDeck.Services;

namespace RunDeck
{
    public partial class App : Application
    {
        private readonly LoggerService _logger = new();

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Capture des exceptions non gérées sur le thread UI
            DispatcherUnhandledException += (sender, args) =>
            {
                _logger.Log(args.Exception);
                MessageBox.Show($"Erreur lors de l'initialisation :\n{args.Exception.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

                args.Handled = true;
                Shutdown(1);
            };

            // Détection du mode CLI
            var silentArgs = SilentArgs.Parse(e.Args);

            if (silentArgs.IsSilent || silentArgs.ShowHelp || silentArgs.ListActions)
            {
                // Attacher une console pour la sortie texte
                AttachConsole();

                var runner = new SilentRunner();
                var exitCode = await runner.RunAsync(silentArgs);
                Shutdown(exitCode);
                return;
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// Attache la console parente (si lancé depuis cmd/powershell)
        /// ou en alloue une nouvelle pour afficher la sortie.
        /// </summary>
        private static void AttachConsole()
        {
            if (!NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS))
                NativeMethods.AllocConsole();
        }
    }

    /// <summary>P/Invoke pour l'attachement console en mode WinExe.</summary>
    internal static class NativeMethods
    {
        internal const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool AttachConsole(uint dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool AllocConsole();
    }
}
