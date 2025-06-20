// ===========================================
// Helpers/DataHelper.cs
// ===========================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Components.AdvancedDataGrid.Models;

namespace Components.AdvancedDataGrid.Helpers
{
    public static class DataHelper
    {
        public static DataTable ConvertToDataTable(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            var dataTable = new DataTable();
            var dataColumns = columns.Where(c => !c.IsSpecialColumn).ToList();

            // Vytvor stĺpce
            foreach (var column in dataColumns)
            {
                dataTable.Columns.Add(column.Name, Nullable.GetUnderlyingType(column.DataType) ?? column.DataType);
            }

            // Pridaj dáta
            foreach (var row in rows.Where(r => !r.IsEmpty))
            {
                var dataRow = dataTable.NewRow();
                foreach (var column in dataColumns)
                {
                    var value = row.GetValue<object>(column.Name);
                    dataRow[column.Name] = value ?? DBNull.Value;
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        public static List<Dictionary<string, object>> ConvertToDictionaries(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            var result = new List<Dictionary<string, object>>();
            var dataColumns = columns.Where(c => !c.IsSpecialColumn).ToList();

            foreach (var row in rows.Where(r => !r.IsEmpty))
            {
                var dict = new Dictionary<string, object>();
                foreach (var column in dataColumns)
                {
                    dict[column.Name] = row.GetValue<object>(column.Name);
                }
                result.Add(dict);
            }

            return result;
        }

        public static object ConvertValue(object value, Type targetType)
        {
            try
            {
                if (value == null)
                    return null;

                if (targetType == typeof(string))
                    return value.ToString();

                if (targetType == typeof(int) || targetType == typeof(int?))
                {
                    if (int.TryParse(value.ToString(), out int intValue))
                        return intValue;
                    return targetType == typeof(int?) ? (int?)null : 0;
                }

                if (targetType == typeof(double) || targetType == typeof(double?))
                {
                    if (double.TryParse(value.ToString(), out double doubleValue))
                        return doubleValue;
                    return targetType == typeof(double?) ? (double?)null : 0.0;
                }

                if (targetType == typeof(bool) || targetType == typeof(bool?))
                {
                    if (bool.TryParse(value.ToString(), out bool boolValue))
                        return boolValue;
                    return targetType == typeof(bool?) ? (bool?)null : false;
                }

                if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                {
                    if (DateTime.TryParse(value.ToString(), out DateTime dateValue))
                        return dateValue;
                    return targetType == typeof(DateTime?) ? (DateTime?)null : DateTime.MinValue;
                }

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }
    }
}