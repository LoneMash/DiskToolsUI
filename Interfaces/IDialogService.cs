// ═══════════════════════════════════════════════════════════════════
// IDialogService.cs — Contrat d'affichage des dialogues utilisateur
// ═══════════════════════════════════════════════════════════════════
// Rôle : Abstrait les MessageBox et dialogues fichier (erreur,
//        avertissement, question, SaveFileDialog) pour le ViewModel.
// Couche : Interfaces
// Consommé par : DialogService, MainWindowViewModel
// ═══════════════════════════════════════════════════════════════════

using System.Windows;

namespace RunDeck.Interfaces
{
    public interface IDialogService
    {
        void ShowError(string message, string title = "Erreur");
        void ShowWarning(string message, string title = "Attention");
        MessageBoxResult ShowQuestion(string message, string title);
        string? ShowSaveFileDialog(string title, string filter, string defaultExt, string fileName);
    }
}
