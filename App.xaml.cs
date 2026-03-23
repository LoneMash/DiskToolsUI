// ═══════════════════════════════════════════════════════════════════
// App.xaml.cs — Point d'entrée et Composition Root de l'application
// ═══════════════════════════════════════════════════════════════════
// Rôle : Configure le conteneur DI (ServiceCollection), détecte le mode
//        CLI (--silent/--help/--list) ou lance la fenêtre principale,
//        et capture les exceptions non gérées.
// Couche : Application
// Consommé par : WPF (point d'entrée), MainWindow, SilentRunner
// ═══════════════════════════════════════════════════════════════════
// App.xaml.cs - Version 4.0
// Changelog :
//   2.3 - Capture des exceptions non gérées via LoggerService
//   3.0 - Branchement du mode silencieux (CLI) via SilentArgs
//         --silent, --all, --list, --help interceptés au démarrage
//   4.0 - Composition Root : enregistrement des services via DI
//         Résolution du ViewModel et de SilentRunner par le conteneur

using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RunDeck.Helpers;
using RunDeck.Interfaces;
using RunDeck.Models;
using RunDeck.Services;
using RunDeck.ViewModels;

namespace RunDeck
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();

            // Services — Singleton : une seule instance pour toute la durée de l'app
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<IConfigService, ConfigService>();
            services.AddSingleton<ICsvExportService, CsvExportService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IResultBuilder, ResultBuilderService>();
            services.AddSingleton<IPowerShellRunner, PowerShellRunner>();

            // Consommateurs — Transient : nouvelle instance à chaque résolution
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SilentRunner>();

            _serviceProvider = services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Doit être fait AVANT toute référence à System.Management.Automation
            // pour que PSVersionInfo.ApplicationBase capte PSHOME au premier accès.
            PathHelper.EnsurePSHome();

            var logger = _serviceProvider.GetRequiredService<ILoggerService>();

            // Capture des exceptions non gérées sur le thread UI
            DispatcherUnhandledException += (sender, args) =>
            {
                // Ignorer silencieusement les ObjectDisposedException à la fermeture
                // (callbacks dispatcher en vol qui accèdent à des ressources déjà libérées)
                if (args.Exception is ObjectDisposedException)
                {
                    args.Handled = true;
                    return;
                }

                logger.Log(args.Exception);
                MessageBox.Show($"Erreur inattendue :\n{args.Exception.Message}",
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

                var runner = _serviceProvider.GetRequiredService<SilentRunner>();
                var exitCode = await runner.RunAsync(silentArgs);
                Shutdown(exitCode);
                return;
            }

            base.OnStartup(e);

            var mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider.Dispose();
            base.OnExit(e);
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
