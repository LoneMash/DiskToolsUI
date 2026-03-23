// ═══════════════════════════════════════════════════════════════════
// SidebarView.xaml.cs — Code-behind de la barre latérale de navigation
// ═══════════════════════════════════════════════════════════════════
// Rôle : Initialise le UserControl de la sidebar listant les actions
//        groupées par catégorie. Logique dans le ViewModel.
// Couche : Views
// Consommé par : MainWindow.xaml (intégré dans le layout gauche)
// ═══════════════════════════════════════════════════════════════════

// SidebarView.xaml.cs - Version 1.0
// Changelog : Code-behind minimal, logique dans MainWindowViewModel

using System.Windows.Controls;

namespace RunDeck.Views
{
    public partial class SidebarView : UserControl
    {
        public SidebarView()
        {
            InitializeComponent();
        }
    }
}
