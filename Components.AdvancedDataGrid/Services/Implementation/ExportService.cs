// ===========================================
// Services/Implementation/ExportService.cs
// ===========================================
using Components.AdvancedDataGrid.Events;
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Components.AdvancedDataGrid.Services.Implementation
{
    public class ExportService : IExportService
    {
        public event EventHandler<ComponentErrorEventArgs> ErrorOccurred;

        public async Task<DataTable> ExportToDataTableAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var dataTable = new DataTable();
                    var dataColumns = columns.Where(c => !c.IsSpecialColumn).ToList();

                    // Vytvor stĺpce
                    foreach (var column in dataColumns)
                    {
                        dataTable.Columns.Add(column.Name, column.DataType);
                    }

                    // Pridaj riadky s dátami
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
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return new DataTable();
            }
        }

        public async Task<string> ExportToCsvAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var sb = new StringBuilder();
                    var dataColumns = columns.Where(c => !c.IsSpecialColumn).ToList();

                    // Header
                    sb.AppendLine(string.Join(",", dataColumns.Select(c => EscapeCsvValue(c.Name))));

                    // Dáta
                    foreach (var row in rows.Where(r => !r.IsEmpty))
                    {
                        var values = dataColumns.Select(c => EscapeCsvValue(row.GetValue<object>(c.Name)?.ToString() ?? ""));
                        sb.AppendLine(string.Join(",", values));
                    }

                    return sb.ToString();
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToCsvAsync"));
                return string.Empty;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            try
            {
                // Pre jednoduchosť vrátime CSV ako bytes
                var csv = await ExportToCsvAsync(rows, columns);
                return Encoding.UTF8.GetBytes(csv);
            }
            catch (Exception ex)
            {
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