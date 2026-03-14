// MainWindow.xaml.cs - Version 2.4
// Changelog : Suppression de l'écriture directe dans error.log
//             Suppression de BuildExceptionMessage (déplacé dans LoggerService)
//             Suppression de WriteLog (déplacé dans LoggerService)
//             Tout passe désormais par LoggerService

using System;
using System.Windows;
using System.Windows.Input;
using RunDeck.Services;
using RunDeck.ViewModels;

namespace RunDeck
{
    public partial class MainWindow : Window
    {
        private readonly LoggerService _logger = new();

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                DataContext = new MainWindowViewModel();
            }
            catch (Exception ex)
            {
                _logger.Log(ex);
                MessageBox.Show($"Erreur constructeur MainWindow :\n\n{ex.Message}",
                    "Erreur fatale", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(1);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal : WindowState.Maximized;
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
