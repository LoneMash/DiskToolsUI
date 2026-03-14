# RunDeck — Documentation technique

> **Version** : 3.0
> **Framework** : .NET 8.0 / WPF
> **Pattern** : MVVM (CommunityToolkit.Mvvm)
> **Dernière mise à jour** : Mars 2026

---

## Table des matières

1. [Présentation](#1-présentation)
2. [Arborescence du projet](#2-arborescence-du-projet)
3. [Architecture et interaction des fichiers](#3-architecture-et-interaction-des-fichiers)
4. [Configuration des actions (actions.json)](#4-configuration-des-actions-actionsjson)
5. [Ajouter une nouvelle action](#5-ajouter-une-nouvelle-action)
6. [Personnaliser les couleurs de l'interface](#6-personnaliser-les-couleurs-de-linterface)
7. [Mode silencieux (CLI)](#7-mode-silencieux-cli)
8. [Compilation et exécution](#8-compilation-et-exécution)

---

## 1. Présentation

RunDeck est une application WPF qui sert de **tableau de bord configurable** pour exécuter des fonctions PowerShell. L'interface se construit dynamiquement à partir de fichiers JSON : les boutons, paramètres et le type d'affichage des résultats sont tous pilotés par la configuration, sans modification du code C#.

**Principes clés** :
- **Un seul script PowerShell** (`Scripts/PowerShell_Base.ps1`) contient toutes les fonctions.
- **`actions.json`** définit les boutons, leurs paramètres et la fonction PS à appeler.
- **L'interface détecte automatiquement** le type de résultat retourné (tableau, clé/valeur, texte brut).
- **Les couleurs sont centralisées** dans un seul fichier (`Resources/Colors.xaml`).

---

## 2. Arborescence du projet

```
RunDeck/
│
├── appsettings.json              # Configuration globale (titre, dimensions, logging)
├── actions.json                  # Définition des boutons et actions PowerShell
│
├── App.xaml / App.xaml.cs        # Point d'entrée, chargement des ressources
├── MainWindow.xaml / .cs         # Fenêtre principale (header, sidebar, contenu)
├── AssemblyInfo.cs               # Métadonnées assembly
│
├── ViewModels/
│   └── MainWindowViewModel.cs    # Logique de présentation (MVVM)
│
├── Views/
│   ├── SidebarView.xaml / .cs    # Sidebar : logo, catégories, boutons d'action
│   ├── ParametersView.xaml / .cs # Formulaire dynamique de paramètres
│   └── ResultsView.xaml / .cs    # Affichage des résultats (KeyValue, Table, Log)
│
├── Models/
│   ├── ActionDefinition.cs       # Modèle d'une action (id, nom, script, paramètres)
│   ├── ParameterDefinition.cs    # Modèle d'un paramètre (nom, type, valeur)
│   ├── ParameterType.cs          # Enum : Text, Number, Bool, Dropdown
│   ├── ResultItem.cs             # Modèle de résultat (+ TableView pour DataGrid)
│   ├── TableRow.cs               # Ligne de tableau
│   ├── AppConfig.cs              # Désérialisation de appsettings.json et actions.json
│   └── SilentArgs.cs             # Arguments du mode CLI silencieux
│
├── Services/
│   ├── ConfigService.cs          # Chargement JSON → modèles applicatifs
│   ├── PowerShellRunner.cs       # Exécution PS + auto-détection du type de sortie
│   ├── LoggerService.cs          # Logging fichier (error.log)
│   ├── CsvExportService.cs       # Export des résultats en CSV
│   └── SilentRunner.cs           # Exécution en mode CLI (sans interface)
│
├── Converters/
│   ├── BoolToVisibilityConverter.cs      # bool → Visible/Collapsed
│   ├── EqualityConverter.cs              # Comparaison pour la sélection sidebar
│   └── TypeToVisibilityConverter.cs      # Affichage conditionnel par type
│
├── Helpers/
│   └── PathHelper.cs             # Résolution des chemins relatifs à l'exe
│
├── Resources/
│   ├── Colors.xaml               # Palette de couleurs (seul fichier à modifier pour le thème)
│   ├── Sizes.xaml                # Dimensions, marges, coins arrondis, polices
│   ├── Buttons.xaml              # Styles des boutons (execute, sidebar, fenêtre)
│   └── Converters.xaml           # Déclaration des converters WPF
│
├── Scripts/
│   └── PowerShell_Base.ps1       # Script unique contenant toutes les fonctions PS
│
├── Icons/
│   ├── app.ico                   # Icône de l'application (multi-résolution)
│   └── app-icon.png              # Icône PNG 256x256
│
├── create-icon.ps1               # Script de génération de l'icône
├── DiskToolsUi.csproj            # Projet .NET (AssemblyName=RunDeck)
└── DiskToolsUi.sln               # Solution Visual Studio
```

---

## 3. Architecture et interaction des fichiers

### Flux de démarrage

```
App.xaml.cs
  └─ OnStartup()
       ├─ Arguments CLI ? → SilentRunner.RunAsync()  (mode silencieux)
       └─ Sinon → MainWindow.xaml
                    └─ DataContext = MainWindowViewModel
                         ├─ ConfigService.LoadConfigAsync()      → appsettings.json
                         ├─ ConfigService.LoadActionsAsync()     → actions.json
                         └─ Affiche la sidebar avec les actions
```

### Flux d'exécution d'une action

```
Utilisateur clique sur un bouton (sidebar)
  └─ SelectActionCommand(action)
       ├─ Affiche le formulaire de paramètres (ParametersView)
       └─ Si aucun paramètre → exécute immédiatement

Utilisateur clique "Exécuter"
  └─ ExecuteActionCommand()
       ├─ Valide les paramètres Required
       ├─ PowerShellRunner.ExecuteActionAsync(action, params)
       │    ├─ Charge PowerShell_Base.ps1 dans le runspace (avec cache)
       │    ├─ Appelle la fonction (ex: Get-DiskInfo -DriveLetter C)
       │    └─ Auto-détecte le type de sortie :
       │         ├─ Hashtable        → KeyValue (grille label/valeur)
       │         ├─ PSCustomObject[] → Table    (DataGrid triable)
       │         └─ String[]         → Log      (texte brut)
       └─ Affiche les résultats (ResultsView)
```

### Interactions clés entre fichiers

| Fichier source | Consomme | Produit |
|---|---|---|
| `appsettings.json` | — | Titre fenêtre, dimensions, chemin log |
| `actions.json` | — | Liste des actions, paramètres, catégories |
| `ConfigService` | appsettings.json, actions.json | `AppConfig`, `List<ActionDefinition>` |
| `PowerShellRunner` | `PowerShell_Base.ps1` | `List<ResultItem>` |
| `MainWindowViewModel` | ConfigService, PowerShellRunner | Données pour les vues |
| `SidebarView` | ViewModel.GroupedActions | Boutons catégorisés |
| `ParametersView` | SelectedAction.Parameters | Formulaire dynamique |
| `ResultsView` | ViewModel.Results | Affichage adaptatif |
| `Colors.xaml` | — | Palette pour toutes les vues |

---

## 4. Configuration des actions (actions.json)

Le fichier `actions.json` est le coeur de la configuration. Chaque entrée du tableau `actions` définit un bouton dans la sidebar.

### Structure d'une action

```json
{
  "id": "identifiant-unique",
  "name": "Nom affiché dans la sidebar",
  "description": "Description affichée dans le header quand l'action est sélectionnée.",
  "scriptPath": "Scripts\\PowerShell_Base.ps1",
  "functionName": "Get-MaFonction",
  "category": "NOM_CATEGORIE",
  "icon": "🔧",
  "parameters": [ ... ]
}
```

### Champs détaillés

| Champ | Type | Obligatoire | Description |
|---|---|---|---|
| `id` | string | Oui | Identifiant unique (utilisé en mode CLI) |
| `name` | string | Oui | Nom affiché dans la sidebar |
| `description` | string | Non | Description affichée dans le header contextuel |
| `scriptPath` | string | Oui | Chemin vers le script PS (relatif à l'exe) |
| `functionName` | string | Oui | Nom de la fonction PS à appeler |
| `category` | string | Non | Catégorie de regroupement dans la sidebar (ex: "DISQUE", "SYSTÈME") |
| `icon` | string | Non | Emoji affiché à côté du nom dans la sidebar |
| `parameters` | array | Oui | Liste des paramètres (peut être vide `[]`) |

### Structure d'un paramètre

```json
{
  "name": "NomTechniquePS",
  "label": "Libellé affiché dans l'interface",
  "type": "Text",
  "defaultValue": "valeur_par_defaut",
  "required": true,
  "options": []
}
```

### Types de paramètres supportés

| Type | Contrôle généré | `options` | Exemple |
|---|---|---|---|
| `Text` | Champ texte | Ignoré | Lettre de lecteur, filtre |
| `Number` | Champ texte (numérique) | Ignoré | Port, taille |
| `Bool` | Case à cocher | Ignoré | Activer/désactiver |
| `Dropdown` | Liste déroulante | Requis | `["Option1", "Option2"]` |

### Auto-détection du type de sortie

Il n'y a **pas de champ `outputType`** dans la configuration. Le type d'affichage est déterminé automatiquement par le `PowerShellRunner` en fonction du type de données retourné par la fonction PowerShell :

| Retour PowerShell | Affichage | Exemple |
|---|---|---|
| `@{ Clé = "Valeur" }` (Hashtable) | **KeyValue** — grille label/valeur | `Get-DiskInfo` |
| `[PSCustomObject]` (plusieurs objets) | **Table** — DataGrid triable | `Get-AllDisks` |
| `String` (texte brut) | **Log** — texte scrollable | Scripts de diagnostic |
| Un seul `PSCustomObject` | **KeyValue** | Objet unique avec propriétés |

---

## 5. Ajouter une nouvelle action

Pour ajouter un bouton à l'interface, **aucun code C# n'est nécessaire**. Deux étapes suffisent :

### Étape 1 — Écrire la fonction PowerShell

Ouvrir `Scripts/PowerShell_Base.ps1` et ajouter une nouvelle fonction :

```powershell
function Get-MonAction {
    param(
        [string]$MonParametre = "valeur_defaut"
    )

    try {
        # Pour un affichage KeyValue, retourner un Hashtable :
        return @{
            "Résultat 1" = "Valeur 1"
            "Résultat 2" = "Valeur 2"
        }

        # Pour un affichage Table, retourner un tableau de PSCustomObject :
        # return @(
        #     [PSCustomObject]@{ Colonne1 = "A"; Colonne2 = "B" },
        #     [PSCustomObject]@{ Colonne1 = "C"; Colonne2 = "D" }
        # )
    }
    catch {
        return @{ Error = "Erreur : $($_.Exception.Message)" }
    }
}
```

### Étape 2 — Déclarer l'action dans actions.json

Ajouter une entrée dans le tableau `actions` :

```json
{
  "id": "mon-action",
  "name": "Mon action",
  "description": "Description de ce que fait cette action.",
  "scriptPath": "Scripts\\PowerShell_Base.ps1",
  "functionName": "Get-MonAction",
  "category": "MA_CATEGORIE",
  "icon": "🔧",
  "parameters": [
    {
      "name": "MonParametre",
      "label": "Mon paramètre",
      "type": "Text",
      "defaultValue": "valeur_defaut",
      "required": false,
      "options": []
    }
  ]
}
```

Relancer l'application — le nouveau bouton apparaît automatiquement.

---

## 6. Personnaliser les couleurs de l'interface

Toute la palette de couleurs est centralisée dans **un seul fichier** :

```
Resources/Colors.xaml
```

Modifier ce fichier suffit à changer l'intégralité du thème de l'application. Aucun autre fichier n'a besoin d'être touché.

### Carte des couleurs

#### Fonds

| Clé | Couleur actuelle | Utilisé pour |
|---|---|---|
| `BackgroundDark` | `#0B0E14` | Fond principal de la fenêtre |
| `BackgroundMedium` | `#1A1F36` | Sidebar (haut du gradient) |
| `BackgroundLight` | `#131825` | Barre de statut, cards secondaires |
| `BackgroundInput` | `#131825` | Fond des champs de saisie |
| `GlassBackground` | `#0D1220` | Fond des cards "glass" (paramètres, résultats) |

#### Accent (couleur principale)

| Clé | Couleur actuelle | Utilisé pour |
|---|---|---|
| `AccentColor` | `#6C5CE7` | Couleur d'accent principale |
| `AccentColorHover` | `#7C6CF7` | Survol des éléments d'accent |
| `AccentColorPressed` | `#5B4BD6` | Clic sur les éléments d'accent |
| `AccentGradient` | `#6C5CE7 → #A855F7` | Bouton Exécuter, en-têtes DataGrid, logo |

#### Texte

| Clé | Couleur actuelle | Utilisé pour |
|---|---|---|
| `TextPrimary` | `#E2E8F0` | Texte principal (valeurs, titres) |
| `TextSecondary` | `#94A3B8` | Texte secondaire (labels, descriptions) |
| `TextMuted` | `#64748B` | Texte discret (catégories, statut) |

#### Bordures

| Clé | Couleur actuelle | Utilisé pour |
|---|---|---|
| `BorderColor` | `#1E2642` | Bordures principales |
| `BorderAccent` | `#2A3358` | Bordures d'accent (champs de saisie) |
| `BorderSubtle` | `#1A2038` | Séparateurs discrets |
| `GlassBorder` | `#1E2642` | Bordures des cards glass |

### Exemple : passer à un thème bleu

Pour changer l'accent violet en bleu, modifier ces lignes dans `Colors.xaml` :

```xml
<!-- Avant (violet) -->
<Color x:Key="AccentStartColor">#6C5CE7</Color>
<Color x:Key="AccentEndColor">#A855F7</Color>
<SolidColorBrush x:Key="AccentColor" Color="#6C5CE7"/>

<!-- Après (bleu) -->
<Color x:Key="AccentStartColor">#2563EB</Color>
<Color x:Key="AccentEndColor">#3B82F6</Color>
<SolidColorBrush x:Key="AccentColor" Color="#2563EB"/>
```

### Autres fichiers de style

| Fichier | Rôle | Quand le modifier |
|---|---|---|
| `Resources/Sizes.xaml` | Dimensions, marges, coins arrondis, polices | Ajuster l'espacement ou la taille de la sidebar |
| `Resources/Buttons.xaml` | Styles des boutons | Modifier la forme ou le comportement des boutons |
| `Resources/Converters.xaml` | Déclaration des converters | Rarement modifié |

---

## 7. Mode silencieux (CLI)

L'application peut être exécutée en ligne de commande sans interface graphique :

```bash
RunDeck.exe --silent --action disk-info --DriveLetter C
```

### Options

| Argument | Description |
|---|---|
| `--silent` | Active le mode CLI (pas d'interface graphique) |
| `--action <id>` | Identifiant de l'action à exécuter (champ `id` dans actions.json) |
| `--export <chemin.csv>` | Exporte les résultats en CSV |
| `--<Param> <valeur>` | Paramètres supplémentaires passés à la fonction PS |

### Exemples

```bash
# Lister les KB Windows
RunDeck.exe --silent --action windows-kb

# Exporter les infos système en CSV
RunDeck.exe --silent --action system-info --export C:\rapport.csv

# Filtrer les KB
RunDeck.exe --silent --action windows-kb --Filter "KB50"
```

---

## 8. Compilation et exécution

### Prérequis

- .NET 8.0 SDK
- Windows 10/11

### Compiler

```bash
dotnet build
```

L'exécutable est généré dans `bin/Debug/net8.0-windows/RunDeck.exe`.

### Publier (autonome)

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### Régénérer l'icône

```bash
powershell -ExecutionPolicy Bypass -File create-icon.ps1
```

### Dépendances NuGet

| Package | Version | Rôle |
|---|---|---|
| CommunityToolkit.Mvvm | 8.4.0 | MVVM source generators |
| Microsoft.PowerShell.SDK | 7.4.1 | Exécution PowerShell |
| System.Management.Automation | 7.4.1 | API PowerShell |
| System.Text.Json | 8.0.5 | Désérialisation JSON |
