# DiskToolsUI Best Practices Refactoring Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Moderniser DiskToolsUI en appliquant les best practices .NET/WPF : suppression du code mort, adoption de CommunityToolkit.Mvvm, injection de dépendances, extraction d'interfaces, et séparation propre UI/ViewModel.

**Architecture:** On garde l'architecture MVVM existante mais on la renforce : les services exposent des interfaces, le ViewModel n'a plus de dépendance directe à `System.Windows` (via `IDialogService`), et CommunityToolkit.Mvvm élimine le boilerplate (`RelayCommand` custom, `OnPropertyChanged` manuel). Le `ResultItem` monolithique est remplacé par une hiérarchie typée.

**Tech Stack:** .NET 8, WPF, CommunityToolkit.Mvvm 8.x, System.Text.Json, PowerShell SDK 7.4.1

---

### Task 1: Nettoyage — Supprimer le code mort (Styles/)

**Contexte:** Le dossier `Styles/` contient 3 fichiers XAML (`colors.xaml`, `Converters.xaml`, `Styles.xaml`) qui ne sont PAS référencés dans `App.xaml`. Seuls les fichiers dans `Resources/` sont chargés. Ce dossier crée de la confusion.

**Files:**
- Delete: `Styles/colors.xaml`
- Delete: `Styles/Converters.xaml`
- Delete: `Styles/Styles.xaml`

**Step 1: Supprimer les fichiers Styles/**

```bash
git rm Styles/colors.xaml Styles/Converters.xaml Styles/Styles.xaml
```

**Step 2: Vérifier que l'app compile toujours**

```bash
dotnet build
```

Expected: BUILD SUCCEEDED (ces fichiers ne sont référencés nulle part)

**Step 3: Commit**

```bash
git add -A Styles/
git commit -m "chore: remove dead Styles/ directory (unused XAML resources)"
```

---

### Task 2: Nettoyage — Ajouter bin/ et obj/ au .gitignore

**Contexte:** Les dossiers `bin/` et `obj/` sont trackés par Git, ce qui pollue l'historique avec des binaires. Le `.gitignore` standard .NET les exclut.

**Files:**
- Modify: `.gitignore`

**Step 1: Vérifier le .gitignore actuel**

```bash
cat .gitignore
```

**Step 2: Ajouter les exclusions manquantes**

S'assurer que ces entrées sont présentes dans `.gitignore` :

```
bin/
obj/
.vs/
.vscode/
*.user
*.suo
```

**Step 3: Retirer les fichiers trackés du cache Git**

```bash
git rm -r --cached bin/ obj/ .vscode/ 2>/dev/null || true
```

**Step 4: Commit**

```bash
git add .gitignore
git commit -m "chore: update .gitignore to exclude bin/, obj/, .vscode/"
```

Note: ne PAS commit les fichiers bin/obj supprimés du cache dans le même commit, cela créerait un diff massif. Faire un commit séparé :

```bash
git add -A
git commit -m "chore: remove tracked bin/obj/vscode files from git history"
```

---

### Task 3: Ajouter CommunityToolkit.Mvvm

**Contexte:** Le projet utilise un `RelayCommand` custom et du boilerplate `INotifyPropertyChanged` manuel. CommunityToolkit.Mvvm fournit des source generators qui éliminent ce code.

**Files:**
- Modify: `DiskToolsUi.csproj`
- Delete: `Models/RelayCommand.cs`
- Modify: `ViewModels/MainWindowViewModel.cs`
- Modify: `Models/ParameterDefinition.cs`

**Step 1: Ajouter le package NuGet**

```bash
dotnet add package CommunityToolkit.Mvvm --version 8.4.0
```

**Step 2: Supprimer RelayCommand.cs custom**

Delete `Models/RelayCommand.cs` — il sera remplacé par `CommunityToolkit.Mvvm.Input.RelayCommand`.

**Step 3: Refactorer MainWindowViewModel avec ObservableObject et attributs**

Remplacer :
```csharp
// AVANT
public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private bool _isLoading;
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
    // ... 15 lignes de boilerplate par propriété ...

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

Par :
```csharp
// APRES
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectActionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteActionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    private bool _hasResults;

    [ObservableProperty]
    private string _windowTitle = "PowerShell UI";

    [ObservableProperty]
    private string _statusMessage = "Initialisation...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormVisible))]
    [NotifyPropertyChangedFor(nameof(ActionDescription))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteActionCommand))]
    private ActionDefinition? _selectedAction;

    private string _lastActionName = string.Empty;
}
```

**Step 4: Convertir les commandes en [RelayCommand]**

```csharp
// AVANT
public RelayCommand SelectActionCommand { get; }
public RelayCommand ExecuteActionCommand { get; }
public RelayCommand ExportCsvCommand { get; }

// constructeur:
SelectActionCommand = new RelayCommand(
    param => OnActionSelected((ActionDefinition)param!),
    param => !IsLoading && param is ActionDefinition
);

// APRES — les commandes sont générées automatiquement par le source generator
[RelayCommand(CanExecute = nameof(CanSelectAction))]
private void OnActionSelected(ActionDefinition action)
{
    Results.Clear();
    HasResults = false;
    StatusMessage = $"Action sélectionnée : {action.Name}";
    SelectedAction = action;
    if (!action.HasParameters)
        _ = ExecuteSelectedActionAsync();
}
private bool CanSelectAction(ActionDefinition? action) => !IsLoading && action != null;

[RelayCommand(CanExecute = nameof(CanExecuteAction))]
private async Task ExecuteSelectedActionAsync() { /* ... */ }
private bool CanExecuteAction() => !IsLoading && SelectedAction != null;

[RelayCommand(CanExecute = nameof(CanExportCsv))]
private void ExportToCsv() { /* ... */ }
private bool CanExportCsv() => HasResults && !IsLoading;
```

Note: `[RelayCommand]` sur une méthode `async Task` génère un `AsyncRelayCommand` qui gère correctement les erreurs (plus de `async void`).

**Step 5: Refactorer ParameterDefinition**

```csharp
// AVANT
public class ParameterDefinition : INotifyPropertyChanged
{
    private string _currentValue = string.Empty;
    public string CurrentValue
    {
        get => _currentValue;
        set { _currentValue = value; OnPropertyChanged(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// APRES
public partial class ParameterDefinition : ObservableObject
{
    [ObservableProperty]
    private string _currentValue = string.Empty;

    // Les autres propriétés restent des auto-properties car elles ne changent pas après initialisation
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ParameterType Type { get; set; } = ParameterType.Text;
    public string DefaultValue { get; set; } = string.Empty;
    public bool Required { get; set; } = false;
    public List<string> Options { get; set; } = new();
}
```

**Step 6: Mettre à jour les bindings XAML**

Dans `SidebarView.xaml`, `ParametersView.xaml`, `MainWindow.xaml` : les noms de commandes changent :
- `SelectActionCommand` → `OnActionSelectedCommand`
- `ExecuteActionCommand` → `ExecuteSelectedActionAsyncCommand`
- `ExportCsvCommand` → `ExportToCsvCommand`

Ou bien renommer les méthodes pour garder les mêmes noms de commande :
- Méthode `SelectAction(ActionDefinition)` → génère `SelectActionCommand`
- Méthode `ExecuteAction()` → génère `ExecuteActionCommand`
- Méthode `ExportCsv()` → génère `ExportCsvCommand`

**Step 7: Build et vérifier**

```bash
dotnet build
```

Expected: BUILD SUCCEEDED

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor: adopt CommunityToolkit.Mvvm — remove boilerplate, use source generators"
```

---

### Task 4: Extraire les interfaces des services

**Contexte:** Aucun service n'expose d'interface. Cela empêche les tests unitaires et viole le principe de dépendance inversée (SOLID-D).

**Files:**
- Create: `Services/IConfigService.cs`
- Create: `Services/IPowerShellRunner.cs`
- Create: `Services/ILoggerService.cs`
- Create: `Services/ICsvExportService.cs`
- Modify: `Services/ConfigService.cs`
- Modify: `Services/PowerShellRunner.cs`
- Modify: `Services/LoggerService.cs`
- Modify: `Services/CsvExportService.cs`

**Step 1: Créer ILoggerService**

```csharp
namespace DiskToolsUi.Services
{
    public interface ILoggerService
    {
        void Log(string message);
        void Log(Exception ex);
        void LogInfo(string message);
        void LogError(string message);
        void LogError(Exception ex);
        void LogError(string message, Exception ex);
    }
}
```

**Step 2: Créer IConfigService**

```csharp
using DiskToolsUi.Models;

namespace DiskToolsUi.Services
{
    public interface IConfigService
    {
        Task<AppConfig> LoadConfigAsync();
        Task<List<ActionDefinition>> LoadActionsAsync(string actionsFilePath);
    }
}
```

**Step 3: Créer IPowerShellRunner**

```csharp
using DiskToolsUi.Models;

namespace DiskToolsUi.Services
{
    public interface IPowerShellRunner : IDisposable
    {
        Task<List<ResultItem>> ExecuteActionAsync(
            ActionDefinition action,
            Dictionary<string, object> parameters);
    }
}
```

**Step 4: Créer ICsvExportService**

```csharp
using DiskToolsUi.Models;

namespace DiskToolsUi.Services
{
    public interface ICsvExportService
    {
        void Export(IEnumerable<ResultItem> results, string filePath);
    }
}
```

**Step 5: Implémenter les interfaces sur les classes existantes**

```csharp
public class LoggerService : ILoggerService { /* existant */ }
public class ConfigService : IConfigService { /* existant */ }
public class PowerShellRunner : IPowerShellRunner { /* existant */ }
public class CsvExportService : ICsvExportService { /* existant */ }
```

**Step 6: Mettre à jour MainWindowViewModel pour utiliser les interfaces**

```csharp
private readonly IConfigService    _configService;
private readonly IPowerShellRunner _psRunner;
private readonly ILoggerService    _logger;
private readonly ICsvExportService _csvExport;
```

Le constructeur par défaut garde les instanciations concrètes pour l'instant (DI viendra après).

**Step 7: Build**

```bash
dotnet build
```

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor: extract service interfaces (IConfigService, IPowerShellRunner, ILoggerService, ICsvExportService)"
```

---

### Task 5: Créer IDialogService — Sortir MessageBox du ViewModel

**Contexte:** Le ViewModel utilise directement `MessageBox.Show()` et `SaveFileDialog`, ce qui crée un couplage fort avec `System.Windows` et rend le ViewModel impossible à tester unitairement.

**Files:**
- Create: `Services/IDialogService.cs`
- Create: `Services/DialogService.cs`
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: Créer IDialogService**

```csharp
namespace DiskToolsUi.Services
{
    public interface IDialogService
    {
        void ShowError(string message, string title = "Erreur");
        void ShowWarning(string message, string title = "Attention");
        void ShowInfo(string message, string title = "Information");
        bool Confirm(string message, string title = "Confirmation");
        string? ShowSaveFileDialog(string filter, string defaultFileName, string? initialDirectory = null);
    }
}
```

**Step 2: Créer DialogService (implémentation WPF)**

```csharp
using System.Windows;
using Microsoft.Win32;

namespace DiskToolsUi.Services
{
    public class DialogService : IDialogService
    {
        public void ShowError(string message, string title = "Erreur")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

        public void ShowWarning(string message, string title = "Attention")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        public void ShowInfo(string message, string title = "Information")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public bool Confirm(string message, string title = "Confirmation")
            => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
               == MessageBoxResult.Yes;

        public string? ShowSaveFileDialog(string filter, string defaultFileName, string? initialDirectory = null)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Exporter les résultats",
                Filter = filter,
                DefaultExt = "csv",
                FileName = defaultFileName,
                InitialDirectory = initialDirectory
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
```

**Step 3: Refactorer MainWindowViewModel**

Remplacer tous les `MessageBox.Show(...)` par des appels à `_dialogService` :

```csharp
// AVANT (InitializeAsync)
MessageBox.Show($"Erreur lors de l'initialisation :\n{ex.Message}",
    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

// APRES
_dialogService.ShowError($"Erreur lors de l'initialisation :\n{ex.Message}");
```

```csharp
// AVANT (ValidateParameters)
MessageBox.Show(
    $"Les paramètres suivants sont obligatoires :\n  • {list}",
    "Paramètres manquants", MessageBoxButton.OK, MessageBoxImage.Warning);

// APRES
_dialogService.ShowWarning($"Les paramètres suivants sont obligatoires :\n  • {list}",
    "Paramètres manquants");
```

```csharp
// AVANT (ExportToCsv) — SaveFileDialog + MessageBox
var dialog = new SaveFileDialog { ... };
if (dialog.ShowDialog() != true) return;
// ...
var open = MessageBox.Show("Ouvrir ?", ..., YesNo, ...);

// APRES
var filePath = _dialogService.ShowSaveFileDialog(
    "Fichier CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
    $"{SanitizeFileName(_lastActionName)}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
if (filePath == null) return;

_csvExport.Export(Results, filePath);
StatusMessage = $"Export réussi : {Path.GetFileName(filePath)}";

if (_dialogService.Confirm($"Export réussi !\n{filePath}\n\nOuvrir le fichier ?", "Export CSV"))
    Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
```

**Step 4: Retirer les `using System.Windows` et `using Microsoft.Win32` du ViewModel**

Le ViewModel ne doit plus avoir aucune référence à `System.Windows`.

**Step 5: Build**

```bash
dotnet build
```

**Step 6: Commit**

```bash
git add -A
git commit -m "refactor: extract IDialogService — remove System.Windows dependency from ViewModel"
```

---

### Task 6: Injection de dépendances avec Microsoft.Extensions.DependencyInjection

**Contexte:** Tous les services sont instanciés manuellement avec `new`. L'injection de dépendances standardise la création d'objets et rend le code testable.

**Files:**
- Modify: `DiskToolsUi.csproj` (ajouter package)
- Modify: `App.xaml.cs` (configurer le conteneur DI)
- Modify: `App.xaml` (retirer StartupUri)
- Modify: `MainWindow.xaml.cs`
- Modify: `ViewModels/MainWindowViewModel.cs` (constructeur avec injection)
- Modify: `Services/SilentRunner.cs` (constructeur avec injection)

**Step 1: Ajouter le package NuGet**

```bash
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.1
```

**Step 2: Configurer le conteneur DI dans App.xaml.cs**

```csharp
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DiskToolsUi.Services;
using DiskToolsUi.ViewModels;

namespace DiskToolsUi
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<IConfigService, ConfigService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddTransient<ICsvExportService, CsvExportService>();
            services.AddTransient<IPowerShellRunner, PowerShellRunner>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();

            // Views
            services.AddTransient<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var logger = _serviceProvider.GetRequiredService<ILoggerService>();

            DispatcherUnhandledException += (sender, args) =>
            {
                logger.Log(args.Exception);
                MessageBox.Show($"Erreur inattendue :\n{args.Exception.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
                Shutdown(1);
            };

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}
```

**Step 3: Retirer StartupUri de App.xaml**

```xml
<!-- AVANT -->
<Application ... StartupUri="MainWindow.xaml">

<!-- APRES -->
<Application ... >
```

**Step 4: Modifier MainWindow.xaml.cs pour recevoir le ViewModel**

```csharp
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    // ... titlebar handlers restent identiques ...

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.Dispose();
        base.OnClosed(e);
    }
}
```

**Step 5: Modifier le constructeur MainWindowViewModel pour injection**

```csharp
public MainWindowViewModel(
    IConfigService configService,
    IPowerShellRunner psRunner,
    ILoggerService logger,
    ICsvExportService csvExport,
    IDialogService dialogService)
{
    _configService = configService;
    _psRunner      = psRunner;
    _logger        = logger;
    _csvExport     = csvExport;
    _dialogService = dialogService;

    // ... rest of constructor ...
    _ = InitializeAsync();
}
```

**Step 6: Modifier SilentRunner pour recevoir les dépendances**

```csharp
public class SilentRunner
{
    private readonly IConfigService _configService;
    private readonly IPowerShellRunner _psRunner;
    private readonly ICsvExportService _csvExport;
    private readonly ILoggerService _logger;

    public SilentRunner(
        IConfigService configService,
        IPowerShellRunner psRunner,
        ICsvExportService csvExport,
        ILoggerService logger)
    {
        _configService = configService;
        _psRunner = psRunner;
        _csvExport = csvExport;
        _logger = logger;
    }
    // ... rest unchanged ...
}
```

**Step 7: Build**

```bash
dotnet build
```

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor: add dependency injection via Microsoft.Extensions.DependencyInjection"
```

---

### Task 7: Refactorer ResultItem en hiérarchie de types

**Contexte:** `ResultItem` utilise des flags booléens (`IsTable`, `IsLog`) pour représenter 3 types différents. Cela viole le principe ouvert/fermé et rend le XAML complexe avec des `DataTrigger` imbriqués.

**Files:**
- Modify: `Models/ResultItem.cs` (réécriture)
- Delete: `Models/TableRow.cs` (fusionné dans ResultItem)
- Modify: `Views/ResultsView.xaml` (DataTemplates typés)
- Modify: `Services/PowerShellRunner.cs:175-270` (ParseKeyValue, ParseTable, ParseLog)
- Modify: `Services/CsvExportService.cs:18-43`
- Modify: `Services/SilentRunner.cs:109-129`

**Step 1: Réécrire ResultItem.cs**

```csharp
using System.Collections.ObjectModel;

namespace DiskToolsUi.Models
{
    public abstract class ResultItem { }

    public class KeyValueResult : ResultItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class TableResult : ResultItem
    {
        public ObservableCollection<string> Columns { get; set; } = new();
        public ObservableCollection<TableRow> Rows { get; set; } = new();
    }

    public class LogResult : ResultItem
    {
        public string RawText { get; set; } = string.Empty;
    }

    public class TableRow
    {
        public ObservableCollection<string> Cells { get; set; } = new();
    }
}
```

Note: `TableRow` est fusionné dans le même fichier car c'est un type étroitement lié.

**Step 2: Mettre à jour PowerShellRunner — méthodes Parse**

```csharp
// ParseKeyValue retourne List<ResultItem> avec des KeyValueResult
items.Add(new KeyValueResult { Label = ..., Value = ... });

// ParseTable retourne List<ResultItem> avec un TableResult
return new List<ResultItem> { new TableResult { Columns = columns, Rows = rows } };

// ParseLog retourne List<ResultItem> avec un LogResult
return new List<ResultItem> { new LogResult { RawText = ... } };
```

**Step 3: Mettre à jour CsvExportService**

```csharp
public void Export(IEnumerable<ResultItem> results, string filePath)
{
    var sb = new StringBuilder();
    foreach (var item in results)
    {
        switch (item)
        {
            case TableResult table:
                sb.AppendLine(string.Join(";", table.Columns));
                foreach (var row in table.Rows)
                    sb.AppendLine(string.Join(";", row.Cells.Select(EscapeCsv)));
                break;

            case KeyValueResult kv:
                sb.AppendLine("Propriété;Valeur");
                sb.AppendLine($"{EscapeCsv(kv.Label)};{EscapeCsv(kv.Value)}");
                break;

            case LogResult log:
                sb.AppendLine(log.RawText);
                break;
        }
    }
    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
}
```

**Step 4: Mettre à jour SilentRunner.PrintResults**

```csharp
private void PrintResults(List<ResultItem> results)
{
    foreach (var item in results)
    {
        switch (item)
        {
            case TableResult table:
                Console.WriteLine(string.Join(" | ", table.Columns));
                Console.WriteLine(new string('-', 80));
                foreach (var row in table.Rows)
                    Console.WriteLine(string.Join(" | ", row.Cells));
                break;

            case LogResult log:
                Console.WriteLine(log.RawText);
                break;

            case KeyValueResult kv:
                Console.WriteLine($"{kv.Label,-30} : {kv.Value}");
                break;
        }
    }
}
```

**Step 5: Mettre à jour ResultsView.xaml — utiliser DataTemplates typés**

Remplacer le DataTemplate unique avec DataTriggers par 3 DataTemplates typés :

```xml
<ItemsControl ItemsSource="{Binding Results}">
    <ItemsControl.Resources>
        <!-- MODE KEY/VALUE -->
        <DataTemplate DataType="{x:Type models:KeyValueResult}">
            <Grid Margin="0,4">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="180"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" Text="{Binding Label}"
                           Foreground="{StaticResource TextSecondary}" FontWeight="SemiBold"/>
                <TextBlock Grid.Column="1" Text="{Binding Value}"
                           Foreground="{StaticResource TextPrimary}" TextWrapping="Wrap"/>
            </Grid>
        </DataTemplate>

        <!-- MODE TABLE -->
        <DataTemplate DataType="{x:Type models:TableResult}">
            <!-- ... headers + rows unchanged ... -->
        </DataTemplate>

        <!-- MODE LOG -->
        <DataTemplate DataType="{x:Type models:LogResult}">
            <Border Margin="0,4" Background="{StaticResource BackgroundDark}"
                    CornerRadius="{StaticResource CornerRadiusCard}"
                    Padding="{StaticResource PaddingCard}">
                <TextBox Text="{Binding RawText, Mode=OneWay}" IsReadOnly="True"
                         Background="Transparent" Foreground="{StaticResource TextPrimary}"
                         BorderThickness="0" FontFamily="Consolas, Courier New"
                         FontSize="{StaticResource FontSizeSmall}"
                         TextWrapping="Wrap" AcceptsReturn="True"/>
            </Border>
        </DataTemplate>
    </ItemsControl.Resources>
</ItemsControl>
```

C'est plus propre : WPF sélectionne automatiquement le bon template selon le type concret.

**Step 6: Supprimer TableRow.cs séparé si il existe**

```bash
git rm Models/TableRow.cs 2>/dev/null || true
```

**Step 7: Build**

```bash
dotnet build
```

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor: replace ResultItem boolean flags with typed hierarchy (KeyValueResult, TableResult, LogResult)"
```

---

### Task 8: Bug fix — CheckBox converter incorrect dans ParametersView

**Contexte:** Dans `ParametersView.xaml:94-98`, le `CheckBox.IsChecked` utilise `BoolToVisibilityConverter` pour convertir une string en bool. C'est le mauvais converter — il convertit en `Visibility`, pas en `bool`.

**Files:**
- Create: `Converters/StringToBoolConverter.cs`
- Modify: `Resources/Converters.xaml` (enregistrer le nouveau converter)
- Modify: `Views/ParametersView.xaml:94-98`

**Step 1: Créer StringToBoolConverter**

```csharp
using System;
using System.Globalization;
using System.Windows.Data;

namespace DiskToolsUi.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s && bool.TryParse(s, out var b) && b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? b.ToString() : "False";
    }
}
```

**Step 2: Enregistrer dans Converters.xaml**

```xml
<converters:StringToBoolConverter x:Key="StringToBoolConverter"/>
```

**Step 3: Corriger ParametersView.xaml**

```xml
<!-- AVANT -->
<Binding Path="CurrentValue"
         UpdateSourceTrigger="PropertyChanged"
         Converter="{StaticResource BoolToVisibilityConverter}"/>

<!-- APRES -->
<Binding Path="CurrentValue"
         UpdateSourceTrigger="PropertyChanged"
         Converter="{StaticResource StringToBoolConverter}"/>
```

**Step 4: Build**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add -A
git commit -m "fix: use correct StringToBoolConverter for CheckBox binding in ParametersView"
```

---

### Task 9: Validation de la configuration

**Contexte:** `ConfigService` désérialise le JSON sans valider que les champs obligatoires sont remplis. Un `actions.json` mal formé crash silencieusement.

**Files:**
- Modify: `Services/ConfigService.cs`

**Step 1: Ajouter une méthode de validation dans ConfigService**

Après le mapping dans `MapToDefinitions`, valider :

```csharp
private static void ValidateAction(ActionDefinition action)
{
    if (string.IsNullOrWhiteSpace(action.Id))
        throw new InvalidOperationException("Action sans Id dans actions.json");
    if (string.IsNullOrWhiteSpace(action.Name))
        throw new InvalidOperationException($"Action '{action.Id}' sans Name");
    if (string.IsNullOrWhiteSpace(action.ScriptPath))
        throw new InvalidOperationException($"Action '{action.Id}' sans ScriptPath");
}
```

**Step 2: Appeler la validation dans MapToDefinitions**

```csharp
// Après la création de l'ActionDefinition
ValidateAction(action);
definitions.Add(action);
```

**Step 3: Build**

```bash
dotnet build
```

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: add actions.json validation in ConfigService"
```

---

### Task 10: Test de build final et vérification de l'application

**Step 1: Clean build complet**

```bash
dotnet clean
dotnet build
```

Expected: BUILD SUCCEEDED, 0 warnings

**Step 2: Lancer l'application**

```bash
dotnet run
```

Vérifier :
- La sidebar charge les actions
- Cliquer sur une action sans paramètre → exécution immédiate
- Cliquer sur une action avec paramètres → formulaire visible
- Remplir et exécuter → résultats affichés
- Export CSV fonctionne

**Step 3: Commit final**

```bash
git add -A
git commit -m "chore: final build verification after best practices refactoring"
```
