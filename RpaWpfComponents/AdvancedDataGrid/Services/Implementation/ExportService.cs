// RpaWpfComponents/AdvancedDataGrid/Services/Implementation/ExportService.cs
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Implementation
{
    internal class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;

        public ExportService(ILogger<ExportService> logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportService>.Instance;
        }

        public event EventHandler<ComponentErrorEventArgs> ErrorOccurred;

        public Task<DataTable> ExportToDataTableAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns, bool includeValidAlerts = false)
        {
            try
            {
                _logger.LogDebug("Exporting {RowCount} rows with {ColumnCount} columns to DataTable, includeValidAlerts: {IncludeValidAlerts}",
                    rows?.Count ?? 0, columns?.Count ?? 0, includeValidAlerts);

                var dataTable = new DataTable();
                var exportColumns = GetExportColumns(columns ?? new List<ColumnDefinitionModel>(), includeValidAlerts);

                foreach (var column in exportColumns)
                {
                    dataTable.Columns.Add(column.Name, column.DataType);
                    _logger.LogTrace("Added column to DataTable: {ColumnName} ({DataType})", column.Name, column.DataType.Name);
                }

                if (rows != null)
                {
                    var dataRows = rows.Where(r => !r.IsEmpty).ToList();

                    foreach (var row in dataRows)
                    {
                        var dataRow = dataTable.NewRow();

                        foreach (var column in exportColumns)
                        {
                            var value = row.GetValue<object>(column.Name);
                            dataRow[column.Name] = value ?? DBNull.Value;
                        }

                        dataTable.Rows.Add(dataRow);
                    }

                    _logger.LogDebug("Added {DataRowCount} data rows to DataTable", dataRows.Count);
                }

                _logger.LogInformation("Successfully exported {RowCount} rows with {ColumnCount} columns to DataTable",
                    dataTable.Rows.Count, dataTable.Columns.Count);
                return Task.FromResult(dataTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return Task.FromResult(new DataTable());
            }
        }

        public Task<string> ExportToCsvAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns, bool includeValidAlerts = false)
        {
            try
            {
                _logger.LogDebug("Exporting {RowCount} rows to CSV format, includeValidAlerts: {IncludeValidAlerts}",
                    rows?.Count ?? 0, includeValidAlerts);

                var sb = new StringBuilder();
                var exportColumns = GetExportColumns(columns ?? new List<ColumnDefinitionModel>(), includeValidAlerts);

                sb.AppendLine(string.Join(",", exportColumns.Select(c => EscapeCsvValue(c.Name))));

                if (rows != null)
                {
                    foreach (var row in rows.Where(r => !r.IsEmpty))
                    {
                        var values = exportColumns.Select(c => EscapeCsvValue(row.GetValue<object>(c.Name)?.ToString() ?? ""));
                        sb.AppendLine(string.Join(",", values));
                    }
                }

                var result = sb.ToString();
                _logger.LogInformation("Successfully exported to CSV, length: {Length}", result.Length);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToCsvAsync"));
                return Task.FromResult(string.Empty);
            }
        }

        public async Task<byte[]> ExportToExcelAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns, bool includeValidAlerts = false)
        {
            try
            {
                _logger.LogDebug("Exporting {RowCount} rows to Excel format, includeValidAlerts: {IncludeValidAlerts}",
                    rows?.Count ?? 0, includeValidAlerts);

                var csv = await ExportToCsvAsync(rows, columns, includeValidAlerts);
                var result = Encoding.UTF8.GetBytes(csv);

                _logger.LogInformation("Successfully exported to Excel format, bytes: {ByteCount}", result.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToExcelAsync"));
                return new byte[0];
            }
        }

        private List<ColumnDefinitionModel> GetExportColumns(List<ColumnDefinitionModel> originalColumns, bool includeValidAlerts)
        {
            var exportColumns = new List<ColumnDefinitionModel>();

            var normalColumns = originalColumns
                .Where(c => c.Name != "DeleteAction" && c.Name != "ValidAlerts")
                .ToList();

            exportColumns.AddRange(normalColumns);

            _logger.LogDebug("Added {NormalColumnCount} normal columns to export", normalColumns.Count);

            if (includeValidAlerts)
            {
                var validAlertsColumn = originalColumns.FirstOrDefault(c => c.Name == "ValidAlerts");
                if (validAlertsColumn != null)
                {
                    exportColumns.Add(validAlertsColumn);
                    _logger.LogDebug("Added ValidAlerts column to export (at end)");
                }
                else
                {
                    _logger.LogWarning("ValidAlerts column requested but not found in original columns");
                }
            }

            _logger.LogInformation("Export columns prepared: {ColumnCount} total, includeValidAlerts: {IncludeValidAlerts}",
                exportColumns.Count, includeValidAlerts);

            return exportColumns;
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}