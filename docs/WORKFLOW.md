# RunDeck — Architecture & Workflow

> Document de référence technique — Mars 2026

---

## 1. Vue d'ensemble

RunDeck est une application WPF (.NET 8) qui exécute des actions PowerShell configurables via un fichier JSON. Elle fonctionne en deux modes :

- **Mode GUI** — Interface graphique avec sidebar, formulaire de paramètres et affichage des résultats en temps réel
- **Mode CLI** — Exécution silencieuse en ligne de commande avec export CSV/JSON

---

## 2. Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     App.xaml.cs                          │
│              (Composition Root — DI Container)           │
│  ┌──────────────────┐    ┌────────────────────────┐     │
│  │   Mode GUI       │    │   Mode CLI             │     │
│  │   MainWindow     │    │   SilentRunner          │     │
│  │   + ViewModel    │    │   (console output)      │     │
│  └────────┬─────────┘    └───────────┬────────────┘     │
└───────────┼──────────────────────────┼──────────────────┘
            │                          │
            ▼                          ▼
┌─────────────────────────────────────────────────────────┐
│                    Couche Services                       │
│  ┌──────────────┐ ┌────────────────┐ ┌───────────────┐  │
│  │ ConfigService│ │PowerShellRunner│ │CsvExportService│  │
│  └──────────────┘ └────────────────┘ └───────────────┘  │
│  ┌──────────────┐ ┌────────────────┐ ┌───────────────┐  │
│  │LoggerService │ │ ThemeService   │ │ DialogService  │  │
│  └──────────────┘ └────────────────┘ └───────────────┘  │
│  ┌──────────────────────────────────┐                    │
│  │      ResultBuilderService       │                    │
│  └──────────────────────────────────┘                    │
└─────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────┐
│                    Couche Modèles                        │
│  ActionDefinition · ParameterDefinition · ResultItem     │
│  KeyValueResult · TableResult · LogResult · AppConfig    │
└─────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────┐
│                  Fichiers de config                      │
│         appsettings.json  ·  actions.json                │
│              Scripts/PowerShell_Base.ps1                  │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Pattern MVVM

```
┌─────────────┐        ┌──────────────────┐        ┌──────────────┐
│    Views     │◄──────►│   ViewModel      │───────►│   Models     │
│  (XAML)      │ Binding│ MainWindowVM     │        │ ResultItem   │
│              │        │                  │        │ ActionDef    │
│ SidebarView  │        │ Commands:        │        │ ParameterDef │
│ ParametersV  │        │  SelectAction    │        └──────────────┘
│ ResultsView  │        │  ExecuteAction   │
│              │        │  ExportCsv       │        ┌──────────────┐
│              │        │  ToggleTheme     │───────►│  Services    │
└─────────────┘        └──────────────────┘        │ (via DI)     │
                                                    └──────────────┘
```

- **Views** — Uniquement du XAML et un code-behind vide. Pas de logique.
- **ViewModel** — Toute la logique de présentation. Communique avec les Views via data binding et avec les Services via injection de dépendances.
- **Models** — Données pures, pas de logique métier.

---

## 4. Injection de dépendances

Toutes les dépendances sont enregistrées dans `App.xaml.cs` (Composition Root) :

```csharp
// Singleton — une seule instance pour toute la durée de l'app
services.AddSingleton<ILoggerService, LoggerService>();
services.AddSingleton<IConfigService, ConfigService>();
services.AddSingleton<ICsvExportService, CsvExportService>();
services.AddSingleton<IThemeService, ThemeService>();
services.AddSingleton<IDialogService, DialogService>();
services.AddSingleton<IResultBuilder, ResultBuilderService>();
services.AddSingleton<IPowerShellRunner, PowerShellRunner>();

// Transient — nouvelle instance à chaque résolution
services.AddTransient<MainWindowViewModel>();
services.AddTransient<SilentRunner>();
```

### Interfaces et implémentations

| Interface | Implémentation | Responsabilité |
|-----------|---------------|----------------|
| `ILoggerService` | `LoggerService` | Écriture de logs dans `error.log` |
| `IConfigService` | `ConfigService` | Chargement de `appsettings.json` et `actions.json` |
| `IPowerShellRunner` | `PowerShellRunner` | Exécution de scripts PS (batch et streaming) |
| `ICsvExportService` | `CsvExportService` | Export des résultats en CSV |
| `IThemeService` | `ThemeService` | Basculement thème sombre/clair |
| `IDialogService` | `DialogService` | MessageBox et SaveFileDialog (abstraction UI) |
| `IResultBuilder` | `ResultBuilderService` | Transformation PSObject → ResultItem typés |

---

## 5. Workflow — Mode GUI (démarrage)

```
Utilisateur lance RunDeck.exe
        │
        ▼
┌─ App.OnStartup() ──────────────────────────────────────┐
│  1. PathHelper.EnsurePSHome()                           │
│     → Définit la variable PSHOME pour PowerShell SDK    │
│                                                         │
│  2. Enregistre le handler d'exceptions globales         │
│                                                         │
│  3. Parse les arguments CLI (SilentArgs.Parse)          │
│     → Si --silent/--help/--list → Mode CLI (section 7)  │
│                                                         │
│  4. Résout MainWindowViewModel depuis le conteneur DI   │
│     → Le conteneur injecte automatiquement les 7        │
│       services dans le constructeur du ViewModel        │
│                                                         │
│  5. Crée MainWindow et assigne DataContext              │
│  6. Affiche la fenêtre                                  │
└─────────────────────────────────────────────────────────┘
        │
        ▼
┌─ MainWindowViewModel.InitializeAsync() ────────────────┐
│  1. StatusMessage = "Chargement de la configuration..." │
│  2. ConfigService.LoadConfigAsync()                     │
│     → Lit appsettings.json → AppConfig                  │
│  3. ConfigService.LoadActionsAsync()                    │
│     → Lit actions.json → List<ActionDefinition>         │
│     → Mappe les ParameterConfig → ParameterDefinition   │
│  4. Crée la vue groupée par catégorie (GroupedActions)  │
│  5. StatusMessage = "Prêt — X action(s)"               │
└─────────────────────────────────────────────────────────┘
        │
        ▼
    Interface prête — l'utilisateur voit la sidebar
```

---

## 6. Workflow — Exécution d'une action (Mode GUI)

```
Utilisateur clique sur une action dans la sidebar
        │
        ▼
┌─ SelectActionCommand ──────────────────────────────────┐
│  1. Désabonne les paramètres de l'action précédente    │
│  2. Efface les résultats précédents                    │
│  3. SelectedAction = action choisie                    │
│  4. S'abonne aux changements de paramètres             │
│     → Validation temps réel (CanExecute)               │
│  5. Si pas de paramètres → exécution immédiate         │
│     Sinon → affiche le formulaire ParametersView       │
└─────────────────────────────────────────────────────────┘
        │
   L'utilisateur remplit les paramètres
   (le bouton Exécuter est grisé tant que les champs
    Required sont vides — validation temps réel)
        │
        ▼
┌─ ExecuteActionCommand ─────────────────────────────────┐
│  1. ValidateParameters()                               │
│     → Si champs requis manquants → ShowWarning + stop  │
│                                                         │
│  2. Annule le streaming précédent (si en cours)        │
│  3. Crée un nouveau CancellationTokenSource            │
│                                                         │
│  4. Construit le dictionnaire des paramètres            │
│     { "DriveLetter": "C", ... }                        │
│                                                         │
│  5. Crée un StreamingContext via ResultBuilder          │
│                                                         │
│  6. Appelle PowerShellRunner.ExecuteActionStreamingAsync│
│     avec un callback OnStreamingObjectReceived          │
└─────────────────────────────────────────────────────────┘
        │
        ▼
┌─ PowerShellRunner ─────────────────────────────────────┐
│                                                         │
│  Script avec FunctionName ?                             │
│  ├─ OUI → Charger le script dans le runspace partagé   │
│  │        (avec cache), puis appeler la fonction        │
│  └─ NON → Créer un runspace isolé,                     │
│           exécuter le script directement                │
│                                                         │
│  Mode streaming :                                       │
│  1. PSDataCollection<PSObject> output                   │
│  2. output.DataAdded += callback                        │
│  3. ps.BeginInvoke() → exécution asynchrone            │
│  4. Chaque PSObject reçu déclenche le callback          │
│  5. CancellationToken → ps.Stop() si annulé            │
└─────────────────────────────────────────────────────────┘
        │
   Pour chaque PSObject reçu :
        │
        ▼
┌─ OnStreamingObjectReceived (ViewModel) ────────────────┐
│  1. Dispatcher.BeginInvoke (thread UI)                  │
│  2. Si _disposed → return (app en fermeture)            │
│  3. Masque le loading overlay                           │
│                                                         │
│  4. ResultBuilder.ProcessStreamingObject()               │
│     ┌──────────────────────────────────────────────┐    │
│     │ Premier objet → détection du type :          │    │
│     │  • Hashtable     → KeyValue (clé/valeur)     │    │
│     │  • String        → Log (texte brut)          │    │
│     │  • PSCustomObject → Table (DataGrid)         │    │
│     │                                              │    │
│     │ Objets suivants → accumulation :             │    │
│     │  • KeyValue : nouveaux KeyValueResult        │    │
│     │  • Log : append au RawText existant          │    │
│     │  • Table : ajout de ligne au DataTable       │    │
│     └──────────────────────────────────────────────┘    │
│                                                         │
│  5. Ajoute les nouveaux items à Results                 │
│  6. Met à jour HasResults et StatusMessage              │
└─────────────────────────────────────────────────────────┘
        │
        ▼
┌─ Affichage dans ResultsView ───────────────────────────┐
│  DataTemplates typés (sélection automatique par WPF) :  │
│                                                         │
│  • KeyValueResult → Grille Label | Valeur              │
│  • TableResult    → DataGrid avec tri natif             │
│  • LogResult      → TextBox monospace (Consolas)        │
│                                                         │
│  Les résultats apparaissent en temps réel               │
│  (streaming ligne par ligne)                            │
└─────────────────────────────────────────────────────────┘
```

---

## 7. Workflow — Mode CLI

```
RunDeck.exe --silent --action disk-info --DriveLetter C --export rapport.csv
        │
        ▼
┌─ App.OnStartup() ─────────────────────────────────────┐
│  1. SilentArgs.Parse(args) détecte --silent            │
│  2. AttachConsole() — attache la console parente       │
│  3. Résout SilentRunner depuis le conteneur DI         │
│  4. SilentRunner.RunAsync(silentArgs)                  │
└─────────────────────────────────────────────────────────┘
        │
        ▼
┌─ SilentRunner.RunAsync() ──────────────────────────────┐
│                                                         │
│  --help  → Affiche l'aide et quitte                    │
│  --list  → Affiche la liste des actions et quitte      │
│  --all   → Exécute TOUTES les actions                  │
│  --action <id> → Exécute UNE action                    │
│                                                         │
│  Pour chaque action :                                   │
│  1. Charge la config (ConfigService)                    │
│  2. Construit les paramètres (CLI > défaut)             │
│  3. Exécute via PowerShellRunner (mode batch)           │
│  4. Affiche les résultats en console                    │
│  5. Si --export → exporte en CSV ou JSON                │
│                                                         │
│  Retourne un code de sortie (0=OK, 1=erreur)           │
└─────────────────────────────────────────────────────────┘
```

---

## 8. Flux de données — Configuration

```
appsettings.json                    actions.json
┌──────────────────┐       ┌────────────────────────────┐
│ UI:              │       │ actions: [                 │
│   Title: RunDeck │       │   {                        │
│   Width: 900     │       │     id: "disk-info",       │
│ ActionsFile:     │       │     name: "Infos disque",  │
│   actions.json   │       │     scriptPath: "...",     │
│ Logging:         │       │     functionName: "...",   │
│   LogFileName    │       │     parameters: [...]      │
└────────┬─────────┘       │   }                        │
         │                 └──────────┬─────────────────┘
         │                            │
         ▼                            ▼
    ConfigService              ConfigService
   .LoadConfigAsync()        .LoadActionsAsync()
         │                            │
         ▼                            ▼
      AppConfig              List<ActionDefinition>
    (UiConfig,                 (avec ParameterDefinition
     LoggingConfig)             mappés depuis JSON)
```

---

## 9. Hiérarchie des résultats

```
              ResultItem (abstract)
                    │
        ┌───────────┼───────────┐
        ▼           ▼           ▼
  KeyValueResult  TableResult  LogResult
  ┌───────────┐  ┌──────────┐  ┌──────────┐
  │ Label     │  │ Columns  │  │ RawText  │
  │ Value     │  │ Rows     │  │ (observable│
  └───────────┘  │ LiveTable│  │  pour     │
                 │ TableView│  │  streaming)│
                 └──────────┘  └──────────┘

  DataTemplate     DataTemplate   DataTemplate
  automatique      automatique    automatique
  (Grid 2 cols)    (DataGrid)     (TextBox mono)
```

---

## 10. Thèmes

```
ThemeService.ToggleTheme()
        │
        ▼
Application.Resources.MergedDictionaries
        │
  ┌─────┴──────┐
  ▼            ▼
Colors.xaml   ColorsLight.xaml
(thème sombre) (thème clair)
  #0B0E14       #F0F2F5
  #6C5CE7       #6C5CE7 (accent identique)
  #E2E8F0       #1A202C

Toutes les couleurs sont référencées via DynamicResource
→ Le changement de thème est instantané, sans rechargement
```

---

## 11. Arborescence du projet

```
RunDeck/
├── App.xaml / App.xaml.cs              # Point d'entrée, Composition Root DI
├── MainWindow.xaml / .xaml.cs          # Fenêtre principale (shell UI)
├── appsettings.json                    # Configuration globale (titre, taille, log)
├── actions.json                        # Définition des actions PowerShell
├── DiskToolsUi.csproj                  # Fichier projet .NET 8
│
├── Interfaces/                         # Contrats de service (abstraction)
│   ├── ILoggerService.cs               #   Logging
│   ├── IConfigService.cs               #   Chargement configuration
│   ├── IPowerShellRunner.cs            #   Exécution PowerShell
│   ├── ICsvExportService.cs            #   Export CSV
│   ├── IThemeService.cs                #   Gestion thème sombre/clair
│   ├── IDialogService.cs              #   MessageBox / SaveFileDialog
│   └── IResultBuilder.cs              #   Transformation PSObject → ResultItem
│
├── Models/                             # Modèles de données (POCO)
│   ├── ActionDefinition.cs             #   Définition d'une action
│   ├── ParameterDefinition.cs          #   Paramètre de formulaire (observable)
│   ├── ParameterType.cs                #   Enum : Text, Number, Bool, Dropdown
│   ├── ResultItem.cs                   #   Hiérarchie : KeyValue/Table/LogResult
│   ├── TableRow.cs                     #   Ligne de tableau (ObservableCollection)
│   ├── AppConfig.cs                    #   Modèles de config JSON
│   └── SilentArgs.cs                   #   Parsing des arguments CLI
│
├── ViewModels/                         # Logique de présentation (MVVM)
│   └── MainWindowViewModel.cs          #   ViewModel principal
│
├── Views/                              # Composants visuels (UserControl XAML)
│   ├── SidebarView.xaml / .cs          #   Sidebar avec actions groupées
│   ├── ParametersView.xaml / .cs       #   Formulaire de paramètres
│   └── ResultsView.xaml / .cs          #   Affichage des résultats
│
├── Services/                           # Logique métier
│   ├── ConfigService.cs                #   Charge appsettings + actions
│   ├── PowerShellRunner.cs             #   Exécute PS en batch ou streaming
│   ├── LoggerService.cs                #   Logs fichier (error.log)
│   ├── CsvExportService.cs             #   Export CSV
│   ├── SilentRunner.cs                 #   Mode CLI complet
│   ├── ThemeService.cs                 #   Basculement thème
│   ├── DialogService.cs                #   Abstraction des dialogues WPF
│   └── ResultBuilderService.cs         #   PSObject → ResultItem typés
│
├── Converters/                         # Convertisseurs WPF (binding XAML)
│   ├── BoolToVisibilityConverter.cs    #   bool → Visible/Collapsed
│   ├── BoolToVisibilityInverseConverter#   bool → Collapsed/Visible
│   ├── EqualityConverter.cs            #   Comparaison multi-valeurs (sidebar)
│   ├── TypeToVisibilityConverter.cs    #   ParameterType → Visible/Collapsed
│   └── RequiredFieldBorderConverter.cs #   Bordure rouge si champ requis vide
│
├── Helpers/                            # Utilitaires
│   ├── PathHelper.cs                   #   Résolution chemins + PSHOME
│   └── AppStrings.cs                   #   Chaînes UI centralisées
│
├── Resources/                          # Dictionnaires de ressources XAML
│   ├── Colors.xaml                     #   Palette thème sombre
│   ├── ColorsLight.xaml                #   Palette thème clair
│   ├── Sizes.xaml                      #   Typographie, marges, dimensions
│   ├── Converters.xaml                 #   Déclaration des converters
│   └── Buttons.xaml                    #   Styles des boutons
│
├── Scripts/                            # Scripts PowerShell
│   └── PowerShell_Base.ps1             #   Fonctions PS des actions
│
├── Icons/
│   └── app.ico                         #   Icône de l'application
│
└── Tests/                              # Tests unitaires (xUnit + Moq)
    ├── RunDeck.Tests.csproj            #   Projet de test
    ├── ResultBuilderServiceTests.cs    #   Tests du ResultBuilderService
    └── MainWindowViewModelTests.cs     #   Tests du ViewModel
```

---

## 12. Cycle de vie de l'application

```
                    ┌──────────────┐
                    │   Démarrage  │
                    └──────┬───────┘
                           │
                    EnsurePSHome()
                    Parse CLI args
                           │
              ┌────────────┼────────────┐
              ▼                         ▼
        Mode GUI                   Mode CLI
              │                         │
     Résout ViewModel           Résout SilentRunner
     Crée MainWindow            Exécute les actions
     InitializeAsync()          Affiche/exporte
              │                         │
     Utilisation normale         Shutdown(exitCode)
              │
         Fermeture
              │
     MainWindow.OnClosed()
     → vm.Dispose()
       → _disposed = true
       → Unsubscribe params
       → Cancel + Dispose CTS
              │
     App.OnExit()
     → _serviceProvider.Dispose()
       → PowerShellRunner.Dispose()
         → Ferme le Runspace
```

---

## 13. Technologies utilisées

| Technologie | Version | Usage |
|------------|---------|-------|
| .NET | 8.0 | Framework |
| WPF | (inclus) | Interface graphique |
| CommunityToolkit.Mvvm | 8.4.0 | ObservableObject, RelayCommand |
| Microsoft.PowerShell.SDK | 7.4.1 | Exécution PowerShell embarquée |
| Microsoft.Extensions.DI | 8.0.1 | Injection de dépendances |
| System.Text.Json | 8.0.5 | Sérialisation JSON |
| xUnit | 2.5.3 | Tests unitaires |
| Moq | 4.20.72 | Mocking pour les tests |
