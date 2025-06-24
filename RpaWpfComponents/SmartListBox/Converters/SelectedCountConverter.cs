// ============================================
// RpaWpfComponents/SmartListBox/Converters/SelectedCountConverter.cs
// ============================================
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using RpaWpfComponents.SmartListBox.Models;

namespace RpaWpfComponents.SmartListBox.Converters
{
    public class SelectedCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<SmartListBoxItem> items)
            {
                return items.Count(i => i.IsSelected);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}