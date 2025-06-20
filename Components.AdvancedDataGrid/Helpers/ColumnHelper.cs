// ===========================================
// Helpers/ColumnHelper.cs
// ===========================================
using System;
using System.Collections.Generic;
using System.Linq;
using Components.AdvancedDataGrid.Models;

namespace Components.AdvancedDataGrid.Helpers
{
    public static class ColumnHelper
    {
        public static List<ColumnDefinitionModel> EnsureUniqueNames(List<ColumnDefinitionModel> columns)
        {
            var result = new List<ColumnDefinitionModel>();
            var nameCount = new Dictionary<string, int>();

            foreach (var column in columns)
            {
                var baseName = string.IsNullOrWhiteSpace(column.Name) ? "Column" : column.Name;
                var uniqueName = baseName;

                if (nameCount.ContainsKey(baseName))
                {
                    nameCount[baseName]++;
                    uniqueName = $"{baseName}_{nameCount[baseName]}";
                }
                else
                {
                    nameCount[baseName] = 0;
                }

                var newColumn = new ColumnDefinitionModel
                {
                    Name = uniqueName,
                    DataType = column.DataType,
                    MinWidth = column.MinWidth,
                    MaxWidth = column.MaxWidth,
                    AllowResize = column.AllowResize,
                    AllowSort = column.AllowSort,
                    IsReadOnly = column.IsReadOnly
                };

                result.Add(newColumn);
            }

            return result;
        }

        public static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        public static ColumnDefinitionModel CreateDeleteActionColumn()
        {
            return new ColumnDefinitionModel
            {
                Name = "DeleteAction",
                DataType = typeof(object),
                MinWidth = 50,
                MaxWidth = 50,
                AllowResize = false,
                AllowSort = false,
                IsReadOnly = true
            };
        }

        public static ColumnDefinitionModel CreateValidAlertsColumn()
        {
            return new ColumnDefinitionModel
            {
                Name = "ValidAlerts",
                DataType = typeof(string),
                MinWidth = 150,
                MaxWidth = 400,
                AllowResize = true,
                AllowSort = false,
                IsReadOnly = true
            };
        }
    }
}