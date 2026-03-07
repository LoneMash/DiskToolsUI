// App.xaml.cs - Version 2.3
// Changelog : Suppression de l'écriture directe dans error.log
//             Tout passe désormais par LoggerService
//             Capture des exceptions non gérées sur le thread UI

using System;
using System.Windows;
using DiskToolsUi.Services;

namespace DiskToolsUi
{
    public partial class App : Application
    {
        private readonly LoggerService _logger = new();

        protected override void OnStartup(StartupEventArgs e)
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

            base.OnStartup(e);
        }
    }
}
