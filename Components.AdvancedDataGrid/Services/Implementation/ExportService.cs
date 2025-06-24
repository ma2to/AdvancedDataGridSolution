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
    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;

        public ExportService(ILogger<ExportService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportService>.Instance;
        }

        public event EventHandler<ComponentErrorEventArgs> ErrorOccurred;

        public async Task<DataTable> ExportToDataTableAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            try
            {
                _logger.LogDebug("Exporting {RowCount} rows with {ColumnCount} columns to DataTable", rows?.Count ?? 0, columns?.Count ?? 0);

                return await Task.Run(() =>
                {
                    var dataTable = new DataTable();
                    var dataColumns = columns?.Where(c => !c.IsSpecialColumn).ToList() ?? new List<ColumnDefinitionModel>();

                    foreach (var column in dataColumns)
                    {
                        dataTable.Columns.Add(column.Name, column.DataType);
                    }

                    if (rows != null)
                    {
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
                    }

                    _logger.LogInformation("Successfully exported {RowCount} rows to DataTable", dataTable.Rows.Count);
                    return dataTable;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return new DataTable();
            }
        }

        public async Task<string> ExportToCsvAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            try
            {
                _logger.LogDebug("Exporting {RowCount} rows to CSV format", rows?.Count ?? 0);

                return await Task.Run(() =>
                {
                    var sb = new StringBuilder();
                    var dataColumns = columns?.Where(c => !c.IsSpecialColumn).ToList() ?? new List<ColumnDefinitionModel>();

                    sb.AppendLine(string.Join(",", dataColumns.Select(c => EscapeCsvValue(c.Name))));

                    if (rows != null)
                    {
                        foreach (var row in rows.Where(r => !r.IsEmpty))
                        {
                            var values = dataColumns.Select(c => EscapeCsvValue(row.GetValue<object>(c.Name)?.ToString() ?? ""));
                            sb.AppendLine(string.Join(",", values));
                        }
                    }

                    var result = sb.ToString();
                    _logger.LogInformation("Successfully exported to CSV, length: {Length}", result.Length);
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToCsvAsync"));
                return string.Empty;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            try
            {
                _logger.LogDebug("Exporting {RowCount} rows to Excel format", rows?.Count ?? 0);

                var csv = await ExportToCsvAsync(rows, columns);
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