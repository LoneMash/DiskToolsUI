// ═══════════════════════════════════════════════════════════════════
// TypeToVisibilityConverter.cs — Affichage conditionnel par type
// ═══════════════════════════════════════════════════════════════════
// Rôle : Compare le type réel d'un paramètre (text, dropdown…) au
//        type attendu via ConverterParameter ; affiche le contrôle
//        correspondant (TextBox ou ComboBox) et masque l'autre.
// Couche : Converters
// Consommé par : ParametersView (bindings XAML des champs dynamiques)
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RunDeck.Converters
{
    public class TypeToVisibilityConverter : IValueConverter
    {
        // ConverterParameter = type attendu ("text" ou "dropdown")
        // Value = type réel du paramètre
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var actualType   = value?.ToString()?.ToLower() ?? "text";
            var expectedType = parameter?.ToString()?.ToLower() ?? "text";
            return actualType == expectedType ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
