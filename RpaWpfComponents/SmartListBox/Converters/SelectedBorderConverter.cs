// ============================================
// RpaWpfComponents/SmartListBox/Converters/SelectedBorderConverter.cs
// ============================================
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RpaWpfComponents.SmartListBox.Converters
{
    public class SelectedBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Color.FromRgb(0, 78, 161)); // Darker blue
            }
            return new SolidColorBrush(Color.FromRgb(200, 200, 200)); // Light gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}