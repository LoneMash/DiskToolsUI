using System.Windows;

namespace DiskToolsUi
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Gestion globale des exceptions
            this.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"ERREUR UI:\n\n{args.Exception}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
