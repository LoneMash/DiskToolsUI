// Version 4.0
// Changelog :
//   1.0 - Initial
//   3.1 - Effacement des résultats dès le clic sur une action
//   4.0 - v3.00 : Utilise ActionDefinition + ParameterDefinition
//                 Utilise ConfigService.LoadActionsAsync()
//                 Utilise PowerShellRunner.ExecuteActionAsync()
//                 Suppression de la dépendance à un script PS unique global
//                 Ajout de validation des paramètres Required avant exécution
//                 Affichage de la description de l'action dans le formulaire

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using DiskToolsUi.Models;
using DiskToolsUi.Services;

namespace DiskToolsUi.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ConfigService    _configService;
        private readonly PowerShellRunner _psRunner;
        private readonly LoggerService    _logger;
        private readonly CsvExportService _csvExport;

        private bool            _isLoading;
        private bool            _hasResults;
        private string          _windowTitle    = "PowerShell UI";
        private string          _statusMessage  = "Initialisation...";
        private string          _lastActionName = string.Empty;
        private ActionDefinition? _selectedAction;

        public MainWindowViewModel()
        {
            _logger        = new LoggerService();
            _configService = new ConfigService();
            _psRunner      = new PowerShellRunner(_logger);
            _csvExport     = new CsvExportService();

            Actions = new ObservableCollection<ActionDefinition>();
            Results = new ObservableCollection<ResultItem>();

            SelectActionCommand = new RelayCommand(
                param => OnActionSelected((ActionDefinition)param!),
                param => !IsLoading && param is ActionDefinition
            );

            ExecuteActionCommand = new RelayCommand(
                async _ => await ExecuteSelectedActionAsync(),
                _ => !IsLoading && SelectedAction != null
            );

            ExportCsvCommand = new RelayCommand(
                _ => ExportToCsv(),
                _ => HasResults && !IsLoading
            );

            _ = InitializeAsync();
        }

        // -----------------------------------------------------------------------
        // Propriétés publiques
        // -----------------------------------------------------------------------
        public ObservableCollection<ActionDefinition> Actions { get; }
        public ObservableCollection<ResultItem>       Results { get; }

        public RelayCommand SelectActionCommand  { get; }
        public RelayCommand ExecuteActionCommand { get; }
        public RelayCommand ExportCsvCommand     { get; }

        public ActionDefinition? SelectedAction
        {
            get => _selectedAction;
            set
            {
                _selectedAction = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFormVisible));
                OnPropertyChanged(nameof(ActionDescription));
                ExecuteActionCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>True si l'action sélectionnée possède des paramètres à afficher</summary>
        public bool IsFormVisible => SelectedAction != null && SelectedAction.HasParameters;

        /// <summary>Description de l'action sélectionnée, affichée dans ParametersView</summary>
        public string ActionDescription => SelectedAction?.Description ?? string.Empty;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                SelectActionCommand.RaiseCanExecuteChanged();
                ExecuteActionCommand.RaiseCanExecuteChanged();
                ExportCsvCommand.RaiseCanExecuteChanged();
            }
        }

        public bool HasResults
        {
            get => _hasResults;
            set
            {
                _hasResults = value;
                OnPropertyChanged();
                ExportCsvCommand.RaiseCanExecuteChanged();
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // -----------------------------------------------------------------------
        // Initialisation asynchrone
        // -----------------------------------------------------------------------
        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Chargement de la configuration...";

                var appConfig = await _configService.LoadConfigAsync();
                WindowTitle   = appConfig.UI.Title;

                StatusMessage = "Chargement des actions...";
                var actions = await _configService.LoadActionsAsync(appConfig.ActionsFile);

                foreach (var action in actions)
                    Actions.Add(action);

                StatusMessage = $"Prêt — {Actions.Count} action(s) disponible(s)";
                _logger.LogInfo($"Initialisation terminée : {Actions.Count} action(s) chargée(s).");
            }
            catch (Exception ex)
            {
                _logger.LogError("Erreur dans InitializeAsync", ex);
                StatusMessage = $"Erreur d'initialisation : {ex.Message}";
                MessageBox.Show($"Erreur lors de l'initialisation :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // -----------------------------------------------------------------------
        // Sélection d'une action dans la sidebar
        // -----------------------------------------------------------------------
        private void OnActionSelected(ActionDefinition action)
        {
            Results.Clear();
            HasResults    = false;
            StatusMessage = $"Action sélectionnée : {action.Name}";
            SelectedAction = action;

            // Pas de paramètres → exécution immédiate
            if (!action.HasParameters)
                _ = ExecuteSelectedActionAsync();
        }

        // -----------------------------------------------------------------------
        // Validation des paramètres Required avant exécution
        // -----------------------------------------------------------------------
        private bool ValidateParameters()
        {
            if (SelectedAction == null) return false;

            var missing = SelectedAction.Parameters
                .Where(p => p.Required && string.IsNullOrWhiteSpace(p.CurrentValue))
                .Select(p => p.Label)
                .ToList();

            if (missing.Count == 0) return true;

            var list = string.Join("\n  • ", missing);
            MessageBox.Show(
                $"Les paramètres suivants sont obligatoires :\n  • {list}",
                "Paramètres manquants",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            StatusMessage = "Paramètres obligatoires manquants.";
            return false;
        }

        // -----------------------------------------------------------------------
        // Exécution de l'action sélectionnée
        // -----------------------------------------------------------------------
        private async Task ExecuteSelectedActionAsync()
        {
            if (SelectedAction == null) return;
            if (!ValidateParameters()) return;

            try
            {
                IsLoading       = true;
                HasResults      = false;
                _lastActionName = SelectedAction.Name;
                StatusMessage   = $"Exécution : {SelectedAction.Name}...";
                Results.Clear();

                var parameters = SelectedAction.Parameters.ToDictionary(
                    p => p.Name,
                    p => (object)p.CurrentValue
                );

                var resultItems = await _psRunner.ExecuteActionAsync(SelectedAction, parameters);

                foreach (var item in resultItems)
                    Results.Add(item);

                HasResults    = Results.Count > 0;
                StatusMessage = HasResults
                    ? $"Terminé : {SelectedAction.Name} — {Results.Count} résultat(s)"
                    : $"Terminé : {SelectedAction.Name} — Aucun résultat";

                _logger.LogInfo($"Action '{SelectedAction.Name}' terminée : {Results.Count} résultat(s).");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur dans ExecuteSelectedActionAsync ({SelectedAction?.Name})", ex);
                StatusMessage = $"Erreur : {ex.Message}";
                MessageBox.Show($"Erreur lors de l'exécution :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // -----------------------------------------------------------------------
        // Export CSV
        // -----------------------------------------------------------------------
        private void ExportToCsv()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title          = "Exporter les résultats en CSV",
                    Filter         = "Fichier CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                    DefaultExt     = "csv",
                    FileName       = $"{SanitizeFileName(_lastActionName)}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (dialog.ShowDialog() != true) return;

                _csvExport.Export(Results, dialog.FileName);
                StatusMessage = $"Export réussi : {Path.GetFileName(dialog.FileName)}";

                var open = MessageBox.Show(
                    $"Export réussi !\n{dialog.FileName}\n\nOuvrir le fichier ?",
                    "Export CSV", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (open == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName        = dialog.FileName,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("Erreur dans ExportToCsv", ex);
                MessageBox.Show($"Erreur lors de l'export :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Where(c => !invalid.Contains(c)).ToArray())
                .Replace(" ", "_").Trim('_');
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose() => _psRunner?.Dispose();
    }
}
