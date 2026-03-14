// EqualityConverter.cs - Version 3.0
// Changelog : Adapté pour le style C# Design Pro
//             Sélection = gradient violet translucide + bordure gauche
//             Défaut = transparent

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RunDeck.Converters
{
    public class EqualityConverter : IMultiValueConverter
    {
        // Couleur active : gradient violet translucide
        private static readonly LinearGradientBrush SelectedBrush = new(
            Color.FromArgb(0x4D, 0x6C, 0x5C, 0xE7),  // #6C5CE7 à 30%
            Colors.Transparent,
            new Point(0, 0.5),
            new Point(1, 0.5));

        // Couleur par défaut : transparent
        private static readonly SolidColorBrush DefaultBrush = Brushes.Transparent;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return DefaultBrush;

            return values[0] != null && values[0] == values[1]
                ? SelectedBrush
                : DefaultBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
