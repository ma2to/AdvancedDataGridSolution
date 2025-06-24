// ============================================
// RpaWpfComponents/SmartListBox/Converters/SelectedBackgroundConverter.cs
// ============================================
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RpaWpfComponents.SmartListBox.Converters
{
    public class SelectedBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Color.FromRgb(0, 120, 215)); // Blue
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}