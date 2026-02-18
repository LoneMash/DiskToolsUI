// TypeToVisibilityConverter.cs - Version 1.0
// Changelog : Affiche/masque TextBox ou ComboBox selon le Type du paramètre (config.json)

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiskToolsUi.Converters
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
