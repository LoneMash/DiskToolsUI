// MainWindowViewModel.cs - Version 2.4
// Changelog : Suppression du dropdown et de LoadAvailableDrivesAsync, TextBox simple

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using DiskToolsUi.Models;
using DiskToolsUi.Services;

namespace DiskToolsUi.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ConfigService _configService;
        private readonly PowerShellRunner _psRunner;
        private readonly LoggerService _logger;
        private AppConfig? _appConfig;

        private bool _isLoading;
        private string _windowTitle = "Disk Tools UI";
        private string _statusMessage = "Initialisation...";

        public MainWindowViewModel()
        {
            _configService = new ConfigService();
            _logger        = new LoggerService();
            _psRunner      = new PowerShellRunner(_logger);

            Actions    = new ObservableCollection<ActionItem>();
            Results    = new ObservableCollection<ResultItem>();
            Parameters = new ObservableCollection<ParameterItem>();

            ExecuteActionCommand = new RelayCommand(
                async param => await ExecuteActionAsync((ActionItem)param!),
                param => !IsLoading && param is ActionItem
            );

            _ = InitializeAsync();
        }

        public ObservableCollection<ActionItem>    Actions    { get; }
        public ObservableCollection<ResultItem>    Results    { get; }
        public ObservableCollection<ParameterItem> Parameters { get; }
        public RelayCommand ExecuteActionCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); ExecuteActionCommand.RaiseCanExecuteChanged(); }
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

                // 1. Lire config.json
                _appConfig  = await _configService.LoadConfigAsync();
                WindowTitle = _appConfig.UI.Title;

                // 2. Charger le script PowerShell
                StatusMessage = "Chargement du script PowerShell...";
                // Dans InitializeAsync(), ligne de chargement du script :
                await _psRunner.LoadScriptAsync(_appConfig.PowerShell.ScriptPath);


                // 3. Construire les paramètres UI (TextBox uniquement)
                foreach (var paramConfig in _appConfig.UI.Parameters)
                {
                    Parameters.Add(new ParameterItem
                    {
                        Name         = paramConfig.Name,
                        Label        = paramConfig.Label,
                        Type         = paramConfig.Type,
                        CurrentValue = paramConfig.Default
                    });
                }

                // 4. Charger les actions
                foreach (var actionConfig in _appConfig.UI.Actions)
                {
                    Actions.Add(new ActionItem
                    {
                        Name         = actionConfig.Name,
                        FunctionName = actionConfig.FunctionName,
                        ResultField  = actionConfig.ResultField
                    });
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

        private async Task ExecuteActionAsync(ActionItem action)
        {
            try
            {
                IsLoading     = true;
                StatusMessage = $"Exécution : {action.Name}...";
                Results.Clear();

                // Passer tous les paramètres courants à la fonction PS
                var parameters = Parameters.ToDictionary(
                    p => p.Name,
                    p => (object)p.CurrentValue
                );

                var result = await _psRunner.ExecuteFunctionAsync(action.FunctionName, parameters);

                foreach (var kvp in result)
                {
                    Results.Add(new ResultItem
                    {
                        Label = kvp.Key,
                        Value = kvp.Value
                    });
                }

                StatusMessage = $"Terminé : {action.Name}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur dans ExecuteActionAsync ({action.Name})", ex);
                StatusMessage = $"Erreur : {ex.Message}";
                MessageBox.Show($"Erreur lors de l'exécution :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose() => _psRunner?.Dispose();
    }
}
