// ═══════════════════════════════════════════════════════════════════
// ThemeService.cs — Bascule entre thème sombre et clair
// ═══════════════════════════════════════════════════════════════════
// Rôle : Permute le ResourceDictionary de couleurs (Colors.xaml /
//        ColorsLight.xaml) dans les MergedDictionaries de l'application.
// Couche : Services
// Consommé par : MainWindowViewModel (commande ToggleTheme)
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Linq;
using System.Windows;

using RunDeck.Interfaces;

namespace RunDeck.Services
{
    public class ThemeService : IThemeService
    {
        private const string DarkUri  = "/Resources/Colors.xaml";
        private const string LightUri = "/Resources/ColorsLight.xaml";

        private bool _isDark = true;

        public bool IsDark => _isDark;

        public void ToggleTheme()
        {
            _isDark = !_isDark;
            ApplyTheme(_isDark ? DarkUri : LightUri);
        }

        private static void ApplyTheme(string themeUri)
        {
            var app = Application.Current;
            var mergedDicts = app.Resources.MergedDictionaries;

            // Trouver et remplacer le dictionnaire Colors (toujours le premier)
            var oldColors = mergedDicts.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.Contains("Colors.xaml") ||
                 d.Source.OriginalString.Contains("ColorsLight.xaml")));

            var newColors = new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Relative)
            };

            if (oldColors != null)
            {
                var index = mergedDicts.IndexOf(oldColors);
                mergedDicts[index] = newColors;
            }
            else
            {
                mergedDicts.Insert(0, newColors);
            }
        }
    }
}
