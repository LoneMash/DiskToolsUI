// ═══════════════════════════════════════════════════════════════════
// DialogService.cs — Abstraction des boîtes de dialogue WPF
// ═══════════════════════════════════════════════════════════════════
// Rôle : Encapsule MessageBox et SaveFileDialog derrière IDialogService
//        pour permettre le test unitaire du ViewModel sans dépendance UI.
// Couche : Services
// Consommé par : MainWindowViewModel
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Windows;
using Microsoft.Win32;
using RunDeck.Interfaces;

namespace RunDeck.Services
{
    public class DialogService : IDialogService
    {
        public void ShowError(string message, string title = "Erreur")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "Attention")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public MessageBoxResult ShowQuestion(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Information);
        }

        public string? ShowSaveFileDialog(string title, string filter, string defaultExt, string fileName)
        {
            var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                DefaultExt = defaultExt,
                FileName = fileName,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
