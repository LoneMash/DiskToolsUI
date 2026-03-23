// ═══════════════════════════════════════════════════════════════════
// MainWindowViewModel.cs — ViewModel principal de l'interface RunDeck
// ═══════════════════════════════════════════════════════════════════
// Rôle : Orchestre la logique UI : chargement des actions, sélection,
//        exécution streaming avec annulation, validation des paramètres,
//        export CSV et bascule de thème. Utilise CommunityToolkit.Mvvm.
// Couche : ViewModels
// Consommé par : MainWindow.xaml (DataContext injecté par App.xaml.cs)
// ═══════════════════════════════════════════════════════════════════
// Version 6.0
// Changelog :
//   1.0 - Initial
//   3.1 - Effacement des résultats dès le clic sur une action
//   4.0 - v3.00 : Utilise ActionDefinition + ParameterDefinition
//                 Utilise ConfigService.LoadActionsAsync()
//                 Utilise PowerShellRunner.ExecuteActionAsync()
//                 Suppression de la dépendance à un script PS unique global
//                 Ajout de validation des paramètres Required avant exécution
//                 Affichage de la description de l'action dans le formulaire
//   5.0 - Adoption CommunityToolkit.Mvvm : ObservableObject, [ObservableProperty], [RelayCommand]
//   6.0 - Streaming temps réel : les résultats Table apparaissent ligne par ligne
//          Compteur temps réel dans la barre de statut

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RunDeck.Helpers;
using RunDeck.Interfaces;
using RunDeck.Models;

namespace RunDeck.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IDisposable
    {
        private readonly IConfigService    _configService;
        private readonly IPowerShellRunner _psRunner;
        private readonly ILoggerService    _logger;
        private readonly ICsvExportService _csvExport;
        private readonly IThemeService     _themeService;
        private readonly IDialogService    _dialogService;
        private readonly IResultBuilder    _resultBuilder;

        private string _lastActionName = string.Empty;
        private CancellationTokenSource? _streamingCts;
        private bool _disposed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SelectActionCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExecuteActionCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
        private bool _isLoading;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
        private bool _hasResults;

        [ObservableProperty]
        private string _windowTitle = "RunDeck";

        [ObservableProperty]
        private string _statusMessage = AppStrings.StatusInitializing;

        [ObservableProperty]
        private string _themeLabel = AppStrings.ThemeLightLabel;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFormVisible))]
        [NotifyPropertyChangedFor(nameof(ActionDescription))]
        [NotifyCanExecuteChangedFor(nameof(ExecuteActionCommand))]
        private ActionDefinition? _selectedAction;

        public MainWindowViewModel(
            ILoggerService logger,
            IConfigService configService,
            IPowerShellRunner psRunner,
            ICsvExportService csvExport,
            IThemeService themeService,
            IDialogService dialogService,
            IResultBuilder resultBuilder)
        {
            _logger        = logger;
            _configService = configService;
            _psRunner      = psRunner;
            _csvExport     = csvExport;
            _themeService  = themeService;
            _dialogService = dialogService;
            _resultBuilder = resultBuilder;

            Actions = new ObservableCollection<ActionDefinition>();
            Results = new ObservableCollection<ResultItem>();

            _ = InitializeAsync();
        }

        // -----------------------------------------------------------------------
        // Propriétés publiques
        // -----------------------------------------------------------------------
        public ObservableCollection<ActionDefinition> Actions { get; }
        public ObservableCollection<ResultItem>       Results { get; }

        /// <summary>Vue groupée par catégorie pour la sidebar</summary>
        public ICollectionView GroupedActions { get; private set; } = null!;

        /// <summary>True si l'action sélectionnée possède des paramètres à afficher</summary>
        public bool IsFormVisible => SelectedAction != null && SelectedAction.HasParameters;

        /// <summary>Description de l'action sélectionnée, affichée dans ParametersView</summary>
        public string ActionDescription => SelectedAction?.Description ?? string.Empty;

        // -----------------------------------------------------------------------
        // Initialisation asynchrone
        // -----------------------------------------------------------------------
        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = AppStrings.StatusLoadingConfig;

                var appConfig = await _configService.LoadConfigAsync();
                WindowTitle   = appConfig.UI.Title;

                StatusMessage = AppStrings.StatusLoadingActions;
                var actions = await _configService.LoadActionsAsync(appConfig.ActionsFile);

                foreach (var action in actions)
                    Actions.Add(action);

                // Créer la vue groupée par catégorie pour la sidebar
                GroupedActions = CollectionViewSource.GetDefaultView(Actions);
                GroupedActions.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ActionDefinition.Category)));
                OnPropertyChanged(nameof(GroupedActions));

                StatusMessage = AppStrings.StatusReady(Actions.Count);
                _logger.LogInfo($"Initialisation terminée : {Actions.Count} action(s) chargée(s).");
            }
            catch (Exception ex)
            {
                _logger.LogError("Erreur dans InitializeAsync", ex);
                StatusMessage = AppStrings.StatusInitError(ex.Message);
                _dialogService.ShowError(AppStrings.ErrorInit(ex.Message));
            }
            finally
            {
                IsLoading = false;
            }
        }

        // -----------------------------------------------------------------------
        // Commandes
        // -----------------------------------------------------------------------

        [RelayCommand(CanExecute = nameof(CanSelectAction))]
        private void SelectAction(ActionDefinition action)
        {
            // Désabonner les paramètres de l'action précédente
            UnsubscribeParameterChanges();

            Results.Clear();
            HasResults    = false;
            StatusMessage = AppStrings.StatusActionSelected(action.Name);
            SelectedAction = action;

            // S'abonner aux changements de paramètres pour la validation temps réel
            SubscribeParameterChanges();

            // Pas de paramètres -> exécution immédiate
            if (!action.HasParameters)
                _ = ExecuteAction();
        }
        private bool CanSelectAction(ActionDefinition? action) => !IsLoading && action != null;

        [RelayCommand(CanExecute = nameof(CanExecuteAction))]
        private async Task ExecuteAction()
        {
            if (SelectedAction == null) return;
            if (!ValidateParameters()) return;

            // Annuler le streaming précédent s'il est encore en cours
            _streamingCts?.Cancel();
            _streamingCts?.Dispose();
            _streamingCts = new CancellationTokenSource();
            var cts = _streamingCts;

            try
            {
                IsLoading       = true;
                HasResults      = false;
                _lastActionName = SelectedAction.Name;
                StatusMessage   = AppStrings.StatusExecuting(SelectedAction.Name);
                Results.Clear();

                var parameters = SelectedAction.Parameters.ToDictionary(
                    p => p.Name,
                    p => (object)p.CurrentValue
                );

                // État du streaming
                var context = _resultBuilder.CreateContext();
                var actionName = SelectedAction.Name;

                await _psRunner.ExecuteActionStreamingAsync(
                    SelectedAction,
                    parameters,
                    obj => OnStreamingObjectReceived(obj, context, actionName),
                    cts.Token);

                // Ne pas mettre à jour le statut si une autre action a pris le relais
                if (IsCancelled(cts)) return;

                HasResults    = Results.Count > 0;
                StatusMessage = HasResults
                    ? AppStrings.StatusDone(actionName, context.ObjectCount)
                    : AppStrings.StatusDoneEmpty(actionName);

                _logger.LogInfo($"Action '{actionName}' terminée : {context.ObjectCount} résultat(s).");
            }
            catch (OperationCanceledException)
            {
                // Annulation normale — une autre action a été lancée
                _logger.LogInfo($"Action '{SelectedAction?.Name}' annulée par l'utilisateur.");
            }
            catch (ObjectDisposedException)
            {
                // Le CTS a été disposé pendant la fermeture de l'app — rien à faire
            }
            catch (Exception ex)
            {
                // Ignorer les erreurs de pipeline stoppé après annulation
                if (IsCancelled(cts)) return;

                _logger.LogError($"Erreur dans ExecuteAction ({SelectedAction?.Name})", ex);
                StatusMessage = AppStrings.StatusError(ex.Message);
                _dialogService.ShowError(AppStrings.ErrorExecution(ex.Message));
            }
            finally
            {
                IsLoading = false;
            }
        }
        private bool CanExecuteAction() =>
            !IsLoading &&
            SelectedAction != null &&
            SelectedAction.Parameters
                .Where(p => p.Required)
                .All(p => !string.IsNullOrWhiteSpace(p.CurrentValue));

        // -----------------------------------------------------------------------
        // Streaming : traitement de chaque PSObject reçu en temps réel
        // -----------------------------------------------------------------------

        private void OnStreamingObjectReceived(PSObject obj, StreamingContext context, string actionName)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                // Ignorer les callbacks si le ViewModel est déjà disposé (fermeture de l'app)
                if (_disposed) return;

                // Dès le premier objet reçu, masquer l'overlay de chargement
                if (IsLoading)
                    IsLoading = false;

                // Déléguer la transformation au ResultBuilder
                var newItems = _resultBuilder.ProcessStreamingObject(obj, context);

                foreach (var item in newItems)
                    Results.Add(item);

                HasResults = Results.Count > 0;
                StatusMessage = AppStrings.StatusExecutingCount(actionName, context.ObjectCount);
            });
        }

        [RelayCommand(CanExecute = nameof(CanExportCsv))]
        private void ExportCsv()
        {
            try
            {
                var fileName = $"{SanitizeFileName(_lastActionName)}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = _dialogService.ShowSaveFileDialog(
                    "Exporter les résultats en CSV",
                    "Fichier CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                    "csv",
                    fileName);

                if (filePath == null) return;

                _csvExport.Export(Results, filePath);
                StatusMessage = AppStrings.StatusExportSuccess(Path.GetFileName(filePath));

                var open = _dialogService.ShowQuestion(
                    AppStrings.ExportOpenQuestion(filePath),
                    AppStrings.ExportTitle);

                if (open == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName        = filePath,
                        UseShellExecute = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("Erreur dans ExportCsv", ex);
                _dialogService.ShowError(AppStrings.ErrorExport(ex.Message));
            }
        }
        private bool CanExportCsv() => HasResults && !IsLoading;

        // -----------------------------------------------------------------------
        // Validation temps réel des paramètres
        // -----------------------------------------------------------------------

        private void SubscribeParameterChanges()
        {
            if (SelectedAction == null) return;
            foreach (var param in SelectedAction.Parameters)
                param.PropertyChanged += OnParameterValueChanged;
        }

        private void UnsubscribeParameterChanges()
        {
            if (SelectedAction == null) return;
            foreach (var param in SelectedAction.Parameters)
                param.PropertyChanged -= OnParameterValueChanged;
        }

        private void OnParameterValueChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ParameterDefinition.CurrentValue))
                ExecuteActionCommand.NotifyCanExecuteChanged();
        }

        // -----------------------------------------------------------------------
        // Helpers
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
            _dialogService.ShowWarning(
                AppStrings.WarningMissingParams(list),
                AppStrings.WarningMissingParamsTitle);

            StatusMessage = AppStrings.StatusMissingParams;
            return false;
        }

        /// <summary>Vérifie l'annulation sans planter si le CTS est déjà disposé.</summary>
        private static bool IsCancelled(CancellationTokenSource? cts)
        {
            try { return cts == null || cts.Token.IsCancellationRequested; }
            catch (ObjectDisposedException) { return true; }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Where(c => !invalid.Contains(c)).ToArray())
                .Replace(" ", "_").Trim('_');
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            _themeService.ToggleTheme();
            ThemeLabel = _themeService.IsDark ? AppStrings.ThemeLightLabel : AppStrings.ThemeDarkLabel;
        }

        public void Dispose()
        {
            _disposed = true;
            UnsubscribeParameterChanges();
            _streamingCts?.Cancel();
            _streamingCts?.Dispose();
            // Le cycle de vie de _psRunner est géré par le conteneur DI
        }
    }
}
