// RpaWpfComponents/AdvancedDataGrid/Converters/CellValidationConverter.cs
using RpaWpfComponents.AdvancedDataGrid.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RpaWpfComponents.AdvancedDataGrid.Converters
{
    internal class CellValidationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 &&
                values[0] is DataGridRowModel row &&
                values[1] is string columnName)
            {
                var cell = row.GetCell(columnName);
                return cell?.HasValidationError ?? false;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}