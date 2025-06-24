// RpaWpfComponents/AdvancedDataGrid/Behaviors/CustomSortingBehavior.cs
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Configuration;
using RpaWpfComponents.AdvancedDataGrid.Collections;

namespace RpaWpfComponents.AdvancedDataGrid.Behaviors
{
    public class CustomSortingBehavior : Behavior<DataGrid>
    {
        private readonly ILogger<CustomSortingBehavior> _logger;

        public CustomSortingBehavior()
        {
            _logger = LoggerFactory.CreateLogger<CustomSortingBehavior>();
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Sorting += OnDataGridSorting;
            _logger.LogDebug("CustomSortingBehavior attached to DataGrid");
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Sorting -= OnDataGridSorting;
            _logger.LogDebug("CustomSortingBehavior detached from DataGrid");
        }

        private void OnDataGridSorting(object sender, DataGridSortingEventArgs e)
        {
            try
            {
                // Zruš default sorting
                e.Handled = true;

                var dataGrid = (DataGrid)sender;
                var column = e.Column;

                // Nastav sort direction
                var direction = (column.SortDirection != ListSortDirection.Ascending)
                    ? ListSortDirection.Ascending
                    : ListSortDirection.Descending;

                column.SortDirection = direction;

                // Vyčisti ostatné sort indikátory
                foreach (var col in dataGrid.Columns)
                {
                    if (col != column)
                        col.SortDirection = null;
                }

                // Aplikuj custom sorting
                ApplyCustomSorting(dataGrid, column, direction);

                _logger.LogDebug("Custom sorting applied for column: {ColumnName}, direction: {Direction}",
                    column.Header, direction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CustomSortingBehavior");
            }
        }

        private void ApplyCustomSorting(DataGrid dataGrid, DataGridColumn column, ListSortDirection direction)
        {
            if (dataGrid.ItemsSource is not ObservableRangeCollection<DataGridRowModel> collection)
            {
                _logger.LogWarning("ItemsSource is not ObservableRangeCollection<DataGridRowModel>");
                return;
            }

            var columnName = column.Header?.ToString();
            if (string.IsNullOrEmpty(columnName))
            {
                _logger.LogWarning("Column name is null or empty");
                return;
            }

            try
            {
                // Rozdeľ na dáta a prázdne riadky
                var dataRows = collection.Where(r => !r.IsEmpty).ToList();
                var emptyRows = collection.Where(r => r.IsEmpty).ToList();

                _logger.LogDebug("Sorting {DataRowCount} data rows, keeping {EmptyRowCount} empty rows at end",
                    dataRows.Count, emptyRows.Count);

                // Sortuj iba dáta
                var sortedDataRows = direction == ListSortDirection.Ascending
                    ? dataRows.OrderBy(r => GetSortValue(r, columnName)).ToList()
                    : dataRows.OrderByDescending(r => GetSortValue(r, columnName)).ToList();

                // Aktualizuj collection: sortované dáta + prázdne riadky na konci
                collection.Clear();

                foreach (var row in sortedDataRows)
                    collection.Add(row);

                foreach (var row in emptyRows)
                    collection.Add(row);

                _logger.LogDebug("Custom sorting completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying custom sorting for column: {ColumnName}", columnName);
            }
        }

        private object GetSortValue(DataGridRowModel row, string columnName)
        {
            try
            {
                var cell = row.GetCell(columnName);
                var value = cell?.Value;

                // Handle null values
                if (value == null) return GetDefaultValueForSort(cell?.DataType);

                // Get target data type from cell
                var targetType = cell?.DataType ?? typeof(string);

                // Convert value to correct type for proper sorting
                return ConvertValueForSorting(value, targetType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sort value for column: {ColumnName}", columnName);
                return string.Empty;
            }
        }

        private object ConvertValueForSorting(object value, Type targetType)
        {
            try
            {
                // Ak je už správny typ, vráť ako je
                if (value.GetType() == targetType)
                    return value;

                var stringValue = value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(stringValue))
                    return GetDefaultValueForSort(targetType);

                // Konverzia podľa typu
                if (targetType == typeof(int) || targetType == typeof(int?))
                {
                    return int.TryParse(stringValue, out int intVal) ? intVal : int.MinValue;
                }

                if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                {
                    return decimal.TryParse(stringValue, out decimal decVal) ? decVal : decimal.MinValue;
                }

                if (targetType == typeof(double) || targetType == typeof(double?))
                {
                    return double.TryParse(stringValue, out double doubleVal) ? doubleVal : double.MinValue;
                }

                if (targetType == typeof(float) || targetType == typeof(float?))
                {
                    return float.TryParse(stringValue, out float floatVal) ? floatVal : float.MinValue;
                }

                if (targetType == typeof(long) || targetType == typeof(long?))
                {
                    return long.TryParse(stringValue, out long longVal) ? longVal : long.MinValue;
                }

                if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                {
                    return DateTime.TryParse(stringValue, out DateTime dateVal) ? dateVal : DateTime.MinValue;
                }

                if (targetType == typeof(bool) || targetType == typeof(bool?))
                {
                    if (bool.TryParse(stringValue, out bool boolVal))
                        return boolVal;

                    // Podpor aj 1/0, yes/no, y/n
                    return stringValue.ToLower() switch
                    {
                        "1" or "true" or "yes" or "y" or "ano" or "áno" => true,
                        "0" or "false" or "no" or "n" or "nie" => false,
                        _ => false
                    };
                }

                // Pre string type
                if (targetType == typeof(string))
                {
                    return stringValue;
                }

                // Fallback - skús Convert.ChangeType
                if (targetType.IsValueType && !targetType.IsGenericType)
                {
                    return Convert.ChangeType(value, targetType);
                }

                // Posledný fallback na string
                return stringValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert value '{Value}' to type {TargetType}", value, targetType);
                return GetDefaultValueForSort(targetType);
            }
        }

        private object GetDefaultValueForSort(Type? targetType)
        {
            if (targetType == null) return string.Empty;

            // Vráť minimum values pre správne sortovanie (null hodnoty na koniec pri ASC)
            return targetType switch
            {
                var t when t == typeof(int) || t == typeof(int?) => int.MinValue,
                var t when t == typeof(decimal) || t == typeof(decimal?) => decimal.MinValue,
                var t when t == typeof(double) || t == typeof(double?) => double.MinValue,
                var t when t == typeof(float) || t == typeof(float?) => float.MinValue,
                var t when t == typeof(long) || t == typeof(long?) => long.MinValue,
                var t when t == typeof(DateTime) || t == typeof(DateTime?) => DateTime.MinValue,
                var t when t == typeof(bool) || t == typeof(bool?) => false,
                _ => string.Empty
            };
        }
    }
}