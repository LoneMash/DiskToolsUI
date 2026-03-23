// ═══════════════════════════════════════════════════════════════════
// RequiredFieldBorderConverter.cs — Bordure de validation des champs
// ═══════════════════════════════════════════════════════════════════
// Rôle : MultiValueConverter qui retourne une bordure rouge si le
//        champ est marqué Required et que sa valeur est vide, ou
//        transparente sinon. Fournit un retour visuel immédiat.
// Couche : Converters
// Consommé par : ParametersView (MultiBinding sur les champs requis)
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RunDeck.Converters
{
    /// <summary>
    /// MultiValueConverter : retourne une bordure rouge si le champ est Required ET vide.
    /// Values[0] = Required (bool), Values[1] = CurrentValue (string).
    /// </summary>
    public class RequiredFieldBorderConverter : IMultiValueConverter
    {
        private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E));
        private static readonly Brush DefaultBrush = Brushes.Transparent;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return DefaultBrush;

            var required = values[0] is bool b && b;
            var currentValue = values[1] as string ?? string.Empty;

            return required && string.IsNullOrWhiteSpace(currentValue)
                ? ErrorBrush
                : DefaultBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
