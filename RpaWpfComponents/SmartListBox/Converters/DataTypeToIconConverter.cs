// ============================================
// RpaWpfComponents/SmartListBox/Converters/DataTypeToIconConverter.cs
// ============================================
using System;
using System.Globalization;
using System.Windows.Data;
using RpaWpfComponents.SmartListBox.Models;

namespace RpaWpfComponents.SmartListBox.Converters
{
    public class DataTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SmartDataType dataType)
            {
                return dataType switch
                {
                    SmartDataType.Text => "📝",
                    SmartDataType.Number => "🔢",
                    SmartDataType.DateTime => "📅",
                    SmartDataType.FilePath => "📄",
                    SmartDataType.Empty => "❌",
                    _ => "❓"
                };
            }
            return "❓";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}