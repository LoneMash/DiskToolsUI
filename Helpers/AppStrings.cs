// ═══════════════════════════════════════════════════════════════════
// AppStrings.cs — Chaînes de texte centralisées de l'interface
// ═══════════════════════════════════════════════════════════════════
// Rôle : Regroupe toutes les chaînes affichées à l'utilisateur
//        (statuts, messages d'erreur, dialogues, thèmes) en un
//        seul endroit pour faciliter la maintenance et la traduction.
// Couche : Helpers
// Consommé par : MainWindowViewModel, vues XAML (via bindings)
// ═══════════════════════════════════════════════════════════════════

namespace RunDeck.Helpers
{
    /// <summary>
    /// Centralise toutes les chaînes UI de l'application.
    /// Facilite la maintenance et prépare une éventuelle localisation.
    /// </summary>
    public static class AppStrings
    {
        // Initialisation
        public const string StatusInitializing     = "Initialisation...";
        public const string StatusLoadingConfig    = "Chargement de la configuration...";
        public const string StatusLoadingActions   = "Chargement des actions...";
        public static string StatusReady(int count) => $"Prêt — {count} action(s) disponible(s)";
        public static string StatusInitError(string msg) => $"Erreur d'initialisation : {msg}";

        // Exécution
        public static string StatusActionSelected(string name) => $"Action sélectionnée : {name}";
        public static string StatusExecuting(string name) => $"Exécution : {name}...";
        public static string StatusExecutingCount(string name, int count) => $"Exécution : {name}... ({count} résultat(s))";
        public static string StatusDone(string name, int count) => $"Terminé : {name} — {count} résultat(s)";
        public static string StatusDoneEmpty(string name) => $"Terminé : {name} — Aucun résultat";
        public static string StatusError(string msg) => $"Erreur : {msg}";
        public const string StatusMissingParams    = "Paramètres obligatoires manquants.";

        // Export
        public static string StatusExportSuccess(string fileName) => $"Export réussi : {fileName}";
        public static string ExportOpenQuestion(string path) => $"Export réussi !\n{path}\n\nOuvrir le fichier ?";
        public const string ExportTitle            = "Export CSV";

        // Dialogues
        public static string ErrorInit(string msg) => $"Erreur lors de l'initialisation :\n{msg}";
        public static string ErrorExecution(string msg) => $"Erreur lors de l'exécution :\n{msg}";
        public static string ErrorExport(string msg) => $"Erreur lors de l'export :\n{msg}";
        public static string WarningMissingParams(string list) => $"Les paramètres suivants sont obligatoires :\n  • {list}";
        public const string WarningMissingParamsTitle = "Paramètres manquants";

        // Sidebar
        public const string SearchPlaceholder = "Rechercher une action...";

        // Thèmes
        public const string ThemeLightLabel = "Thème clair";
        public const string ThemeDarkLabel  = "Thème sombre";
    }
}
