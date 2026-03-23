// ═══════════════════════════════════════════════════════════════════
// ResultsView.xaml.cs — Code-behind du panneau de résultats
// ═══════════════════════════════════════════════════════════════════
// Rôle : Initialise le UserControl d'affichage des résultats
//        (clé/valeur, tableau, log). Logique dans le ViewModel.
// Couche : Views
// Consommé par : MainWindow.xaml (intégré dans le layout central)
// ═══════════════════════════════════════════════════════════════════

// ResultsView.xaml.cs - Version 1.0
// Changelog : Code-behind minimal, logique dans MainWindowViewModel

using System.Windows.Controls;

namespace RunDeck.Views
{
    public partial class ResultsView : UserControl
    {
        public ResultsView()
        {
            InitializeComponent();
        }
    }
}
