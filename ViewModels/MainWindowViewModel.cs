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
        private AppConfig? _appConfig;
        
        private bool _isLoading;
        private string _driveLetter = "C:";
        private string _statusMessage = "Prêt";

        public MainWindowViewModel()
        {
            _configService = new ConfigService();
            _psRunner = new PowerShellRunner();
            
            Actions = new ObservableCollection<ActionItem>();
            Results = new ObservableCollection<ResultItem>();
            
            ExecuteActionCommand = new RelayCommand(
                async param => await ExecuteActionAsync((ActionItem)param!),
                param => !IsLoading && param is ActionItem
            );
            
            _ = InitializeAsync();
        }

        public ObservableCollection<ActionItem> Actions { get; }
        public ObservableCollection<ResultItem> Results { get; }
        public RelayCommand ExecuteActionCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                ExecuteActionCommand.RaiseCanExecuteChanged();
            }
        }

        public string DriveLetter
        {
            get => _driveLetter;
            set
            {
                _driveLetter = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Chargement de la configuration...";

                _appConfig = await _configService.LoadConfigAsync();

                // Charger le script PowerShell
                await _psRunner.LoadScriptAsync(_appConfig.PowerShell.FunctionsScriptPath);

                // Charger les actions depuis la config
                foreach (var actionConfig in _appConfig.UI.Actions)
                {
                    Actions.Add(new ActionItem
                    {
                        Name = actionConfig.Name,
                        FunctionName = actionConfig.FunctionName,
                        ResultField = actionConfig.ResultField
                    });
                }

                // Charger les paramètres par défaut
                var driveParam = _appConfig.UI.Parameters.FirstOrDefault(p => p.Name == "DriveLetter");
                if (driveParam != null)
                {
                    DriveLetter = driveParam.Default;
                }

                StatusMessage = "Prêt";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur d'initialisation: {ex.Message}";
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
                IsLoading = true;
                StatusMessage = $"Exécution: {action.Name}...";
                Results.Clear();

                var parameters = new Dictionary<string, object>
                {
                    { "DriveLetter", DriveLetter }
                };

                var result = await _psRunner.ExecuteFunctionAsync(action.FunctionName, parameters);

                foreach (var kvp in result)
                {
                    Results.Add(new ResultItem
                    {
                        Label = kvp.Key,
                        Value = kvp.Value
                    });
                }

                StatusMessage = $"Terminé: {action.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur: {ex.Message}";
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
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _psRunner?.Dispose();
        }
    }
}
