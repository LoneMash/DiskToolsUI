// MainWindow.xaml.cs - Version 2.1
// Changelog : Ajout try/catch détaillé dans le constructeur pour diagnostiquer l'erreur fatale UI

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DiskToolsUi.ViewModels;

namespace DiskToolsUi
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                DataContext = new MainWindowViewModel();
            }
            catch (Exception ex)
            {
                // Capture l'exception complète avec inner exceptions
                var logPath = Path.Combine(AppContext.BaseDirectory, "error.log");
                var msg = BuildExceptionMessage(ex);
                File.AppendAllText(logPath, msg);

                MessageBox.Show(
                    $"Erreur constructeur MainWindow :\n\n{msg}",
                    "Erreur fatale",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                throw;
            }
        }

        private string BuildExceptionMessage(Exception ex, int level = 0)
        {
            var indent = new string(' ', level * 2);
            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 💥 EXCEPTION (niveau {level})\n" +
                      $"{indent}Type       : {ex.GetType().FullName}\n" +
                      $"{indent}Message    : {ex.Message}\n" +
                      $"{indent}StackTrace : {ex.StackTrace}\n";

            if (ex.InnerException != null)
                msg += $"{indent}--- INNER EXCEPTION ---\n" +
                       BuildExceptionMessage(ex.InnerException, level + 1);

            msg += new string('-', 80) + "\n";
            return msg;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            else
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close();

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
                vm.Dispose();
            base.OnClosed(e);
        }
    }
}
