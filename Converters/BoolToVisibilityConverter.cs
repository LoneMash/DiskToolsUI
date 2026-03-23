// ═══════════════════════════════════════════════════════════════════
// BoolToVisibilityConverter.cs — Conversion booléen ↔ Visibility
// ═══════════════════════════════════════════════════════════════════
// Rôle : Fournit deux convertisseurs WPF : l'un affiche quand true
//        (BoolToVisibilityConverter), l'autre masque quand true
//        (BoolToVisibilityInverseConverter). Utilisés dans les vues
//        pour afficher/masquer des panneaux selon l'état du modèle.
// Couche : Converters
// Consommé par : ResultsView, ParametersView (bindings XAML)
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RunDeck.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
