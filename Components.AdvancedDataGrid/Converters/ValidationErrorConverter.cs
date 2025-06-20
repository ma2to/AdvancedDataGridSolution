// ===========================================
// Converters/ValidationErrorConverter.cs
// ===========================================
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Components.AdvancedDataGrid.Converters
{
    public class ValidationErrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasErrors)
            {
                if (targetType == typeof(Brush))
                {
                    return hasErrors ? Brushes.Red : Brushes.Transparent;
                }
                else if (targetType == typeof(Thickness))
                {
                    return hasErrors ? new Thickness(2) : new Thickness(0);
                }
                else if (targetType == typeof(Visibility))
                {
                    return hasErrors ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}