// ===========================================
// Services/Implementation/ColumnService.cs
// ===========================================
using Components.AdvancedDataGrid.Events;
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Components.AdvancedDataGrid.Services.Implementation
{
    public class ColumnService : IColumnService
    {
        public event EventHandler<ComponentErrorEventArgs> ErrorOccurred;

        public List<ColumnDefinitionModel> ProcessColumnDefinitions(List<ColumnDefinitionModel> columns)
        {
            try
            {
                var processedColumns = new List<ColumnDefinitionModel>();
                var existingNames = new List<string>();

                foreach (var column in columns)
                {
                    var uniqueName = GenerateUniqueColumnName(column.Name, existingNames);
                    var processedColumn = new ColumnDefinitionModel
                    {
                        Name = uniqueName,
                        DataType = column.DataType,
                        MinWidth = column.MinWidth,
                        MaxWidth = column.MaxWidth,
                        AllowResize = column.AllowResize,
                        AllowSort = column.AllowSort,
                        IsReadOnly = column.IsReadOnly
                    };

                    processedColumns.Add(processedColumn);
                    existingNames.Add(uniqueName);
                }

                return processedColumns;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ProcessColumnDefinitions"));
                return columns;
            }
        }

        public string GenerateUniqueColumnName(string baseName, List<string> existingNames)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseName))
                    baseName = "Column";

                var uniqueName = baseName;
                var counter = 1;

                while (existingNames.Contains(uniqueName))
                {
                    uniqueName = $"{baseName}_{counter}";
                    counter++;
                }

                return uniqueName;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "GenerateUniqueColumnName"));
                return baseName ?? "Column";
            }
        }

        public ColumnDefinitionModel CreateDeleteActionColumn()
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

        public ColumnDefinitionModel CreateValidAlertsColumn()
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

        public bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}