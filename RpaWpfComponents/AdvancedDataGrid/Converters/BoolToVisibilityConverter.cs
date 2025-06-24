// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Converters/BoolToVisibilityConverter.cs
// ===========================================
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RpaWpfComponents.AdvancedDataGrid.Converters
{
    internal class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (Invert)
                    boolValue = !boolValue;

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                var result = visibility == Visibility.Visible;
                return Invert ? !result : result;
            }

            return false;
        }
    }
}