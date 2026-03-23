// ═══════════════════════════════════════════════════════════════════
// MainWindow.xaml.cs — Code-behind de la fenêtre principale
// ═══════════════════════════════════════════════════════════════════
// Rôle : Gère les interactions chrome (drag, minimize, close) et le
//        cycle de vie de la fenêtre. Toute la logique métier est dans
//        le ViewModel ; ce fichier reste volontairement minimal.
// Couche : Application
// Consommé par : App.xaml.cs (instanciation et injection du DataContext)
// ═══════════════════════════════════════════════════════════════════
// MainWindow.xaml.cs - Version 3.0
// Changelog : Suppression de l'écriture directe dans error.log
//             Suppression de BuildExceptionMessage (déplacé dans LoggerService)
//             Suppression de WriteLog (déplacé dans LoggerService)
//             Tout passe désormais par LoggerService
//   3.0 - DataContext injecté par App.xaml.cs via le conteneur DI
//         Suppression de l'instanciation directe du ViewModel

using System;
using System.Windows;
using System.Windows.Input;
using RunDeck.ViewModels;

namespace RunDeck
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
