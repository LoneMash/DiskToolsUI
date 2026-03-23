// ═══════════════════════════════════════════════════════════════════
// IThemeService.cs — Contrat de gestion du thème visuel (clair/sombre)
// ═══════════════════════════════════════════════════════════════════
// Rôle : Expose l'état du thème actif et permet de basculer entre
//        le thème clair et le thème sombre à chaud.
// Couche : Interfaces
// Consommé par : ThemeService, MainWindowViewModel, SidebarView
// ═══════════════════════════════════════════════════════════════════

namespace RunDeck.Interfaces
{
    public interface IThemeService
    {
        bool IsDark { get; }
        void ToggleTheme();
    }
}
