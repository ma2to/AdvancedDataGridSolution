// RpaWpfComponents/AdvancedDataGrid/Services/Implementation/ColumnService.cs
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Implementation
{
    internal class ColumnService : IColumnService
    {
        private readonly ILogger<ColumnService> _logger;

        public ColumnService(ILogger<ColumnService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ColumnService>.Instance;
        }

        public event EventHandler<ComponentErrorEventArgs> ErrorOccurred;

        public List<ColumnDefinitionModel> ProcessColumnDefinitions(List<ColumnDefinitionModel> columns)
        {
            try
            {
                _logger.LogDebug("Processing {Count} column definitions", columns?.Count ?? 0);

                var processedColumns = new List<ColumnDefinitionModel>();
                var existingNames = new List<string>();

                foreach (var column in columns ?? new List<ColumnDefinitionModel>())
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

                _logger.LogInformation("Successfully processed {Count} column definitions", processedColumns.Count);
                return processedColumns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing column definitions");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ProcessColumnDefinitions"));
                return columns ?? new List<ColumnDefinitionModel>();
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

                _logger.LogDebug("Generated unique column name: {UniqueName} from base: {BaseName}", uniqueName, baseName);
                return uniqueName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating unique column name for base: {BaseName}", baseName);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "GenerateUniqueColumnName"));
                return baseName ?? "Column";
            }
        }

        public ColumnDefinitionModel CreateDeleteActionColumn()
        {
            _logger.LogDebug("Creating DeleteAction column");
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
            _logger.LogDebug("Creating ValidAlerts column");
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
            var isSpecial = columnName == "DeleteAction" || columnName == "ValidAlerts";
            _logger.LogTrace("Column {ColumnName} is special: {IsSpecial}", columnName, isSpecial);
            return isSpecial;
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}