// ===========================================
// Converters/ErrorBorderConverter.cs
// ===========================================
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Components.AdvancedDataGrid.Converters
{
    public class ErrorBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasErrors && hasErrors)
            {
                if (targetType == typeof(Brush))
                    return Brushes.Red;
                else if (targetType == typeof(Thickness))
                    return new Thickness(2);
                else if (targetType == typeof(Brush) && parameter?.ToString() == "Background")
                    return new SolidColorBrush(Color.FromArgb(30, 255, 0, 0));
            }

            if (targetType == typeof(Brush))
                return Brushes.Transparent;
            else if (targetType == typeof(Thickness))
                return new Thickness(1);

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}