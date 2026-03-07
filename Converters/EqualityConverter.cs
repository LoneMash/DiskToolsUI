// EqualityConverter.cs - Version 2.0
// Changelog : Retourne un Brush directement (couleur sélectionnée ou couleur normale)
//             au lieu d'un bool, pour pouvoir binder sur Button.Background

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiskToolsUi.Converters
{
    public class EqualityConverter : IMultiValueConverter
    {
        // Couleur du bouton quand il est sélectionné
        private static readonly SolidColorBrush SelectedBrush  = new(Color.FromRgb(0x00, 0x7A, 0xCC)); // #007ACC
        // Couleur du bouton par défaut
        private static readonly SolidColorBrush DefaultBrush   = new(Color.FromRgb(0x2D, 0x2D, 0x30)); // #2D2D30

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = SelectedAction (depuis le ViewModel)
            // values[1] = ActionItem courant (depuis le DataTemplate)
            if (values.Length < 2) return DefaultBrush;

            return values[0] != null && values[0] == values[1]
                ? SelectedBrush
                : DefaultBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
