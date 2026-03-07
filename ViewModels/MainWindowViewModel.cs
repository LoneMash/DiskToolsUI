// MainWindowViewModel.cs - Version 3.1
// Changelog : OnActionSelected — effacement des résultats dès le clic sur une action

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
        private readonly ConfigService _configService;
        private readonly PowerShellRunner _psRunner;
        private readonly LoggerService _logger;
        private readonly CsvExportService _csvExport;
        private AppConfig? _appConfig;

        private bool _isLoading;
        private bool _hasResults;
        private string _windowTitle = "PowerShell UI";
        private string _statusMessage = "Initialisation...";
        private string _lastActionName = string.Empty;
        private ActionItem? _selectedAction;

        public MainWindowViewModel()
        {
            _configService = new ConfigService();
            _logger        = new LoggerService();
            _psRunner      = new PowerShellRunner(_logger);
            _csvExport     = new CsvExportService();

            Actions = new ObservableCollection<ActionItem>();
            Results = new ObservableCollection<ResultItem>();

            SelectActionCommand = new RelayCommand(
                param => OnActionSelected((ActionItem)param!),
                param => !IsLoading && param is ActionItem
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

        public ObservableCollection<ActionItem> Actions { get; }
        public ObservableCollection<ResultItem> Results { get; }

        public RelayCommand SelectActionCommand { get; }
        public RelayCommand ExecuteActionCommand { get; }
        public RelayCommand ExportCsvCommand { get; }

        public ActionItem? SelectedAction
        {
            get => _selectedAction;
            set
            {
                _selectedAction = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFormVisible));
                ExecuteActionCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsFormVisible => SelectedAction != null && SelectedAction.HasParameters;

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

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading     = true;
                StatusMessage = "Chargement de la configuration...";

                _appConfig  = await _configService.LoadConfigAsync();
                WindowTitle = _appConfig.UI.Title;

                StatusMessage = "Chargement du script PowerShell...";
                await _psRunner.LoadScriptAsync(_appConfig.PowerShell.ScriptPath);

                foreach (var actionConfig in _appConfig.UI.Actions)
                {
                    var actionItem = new ActionItem
                    {
                        Name         = actionConfig.Name,
                        FunctionName = actionConfig.FunctionName
                    };

                    foreach (var paramConfig in actionConfig.Parameters)
                    {
                        actionItem.Parameters.Add(new ParameterItem
                        {
                            Name         = paramConfig.Name,
                            Label        = paramConfig.Label,
                            Type         = paramConfig.Type,
                            CurrentValue = paramConfig.Default
                        });
                    }

                    Actions.Add(actionItem);
                }

                StatusMessage = "Prêt";
            }
            catch (Exception ex)
            {
                _logger.LogError("Erreur dans InitializeAsync", ex);
                StatusMessage = $"Erreur : {ex.Message}";
                MessageBox.Show($"Erreur lors de l'initialisation :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ---------------------------------------------------------------
        // MODIFIÉ V3.1 : effacement des résultats et du statut dès le clic
        // ---------------------------------------------------------------
        private void OnActionSelected(ActionItem action)
        {
            // Effacer les résultats précédents dès la sélection d'une nouvelle action
            Results.Clear();
            HasResults    = false;
            StatusMessage = "Prêt";

            SelectedAction = action;

            if (!action.HasParameters)
            {
                // Pas de paramètres → exécution immédiate
                _ = ExecuteSelectedActionAsync();
            }
            // Avec paramètres → IsFormVisible = true → formulaire affiché
        }
        // ---------------------------------------------------------------

        private async Task ExecuteSelectedActionAsync()
        {
            if (SelectedAction == null) return;

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

                var resultItems = await _psRunner.ExecuteFunctionAsync(
                    SelectedAction.FunctionName, parameters);

                foreach (var item in resultItems)
                    Results.Add(item);

                HasResults    = Results.Count > 0;
                StatusMessage = $"Terminé : {SelectedAction.Name}";
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

        private void ExportToCsv()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title            = "Exporter les résultats en CSV",
                    Filter           = "Fichier CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                    DefaultExt       = "csv",
                    FileName         = $"{SanitizeFileName(_lastActionName)}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
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

        private string SanitizeFileName(string name)
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
