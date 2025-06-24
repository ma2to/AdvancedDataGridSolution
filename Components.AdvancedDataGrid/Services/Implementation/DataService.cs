// RpaWpfComponents/AdvancedDataGrid/Services/Implementation/DataService.cs
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Helpers;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Implementation
{
    public class DataService : IDataService
    {
        private readonly ILogger<DataService> _logger;
        private List<DataGridRowModel> _rows = new();
        private List<ColumnDefinitionModel> _columns = new();

        public DataService(ILogger<DataService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DataService>.Instance;
        }

        public event EventHandler<DataChangeEventArgs>? DataChanged;
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        public void Initialize(List<ColumnDefinitionModel> columns)
        {
            try
            {
                _columns = columns ?? throw new ArgumentNullException(nameof(columns));
                _logger.LogInformation("DataService initialized with {ColumnCount} columns", _columns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing DataService");
                throw;
            }
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                _logger.LogInformation("Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);

                await Task.Run(() =>
                {
                    _rows.Clear();

                    if (dataTable != null)
                    {
                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            var gridRow = new DataGridRowModel();

                            foreach (var column in _columns)
                            {
                                var cell = new DataGridCellModel
                                {
                                    ColumnName = column.Name,
                                    DataType = column.DataType
                                };

                                if (dataTable.Columns.Contains(column.Name))
                                {
                                    cell.Value = dataRow[column.Name];
                                }

                                gridRow.AddCell(column.Name, cell);
                            }

                            _rows.Add(gridRow);
                        }
                    }
                });

                _logger.LogInformation("Successfully loaded {RowCount} rows from DataTable", _rows.Count);
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.LoadData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        public async Task LoadDataAsync(List<Dictionary<string, object>> data)
        {
            try
            {
                _logger.LogInformation("Loading data from dictionary list with {RowCount} rows", data?.Count ?? 0);

                await Task.Run(() =>
                {
                    _rows.Clear();

                    if (data != null)
                    {
                        foreach (var dataRow in data)
                        {
                            var gridRow = new DataGridRowModel();

                            foreach (var column in _columns)
                            {
                                var cell = new DataGridCellModel
                                {
                                    ColumnName = column.Name,
                                    DataType = column.DataType
                                };

                                if (dataRow.ContainsKey(column.Name))
                                {
                                    cell.Value = dataRow[column.Name];
                                }

                                gridRow.AddCell(column.Name, cell);
                            }

                            _rows.Add(gridRow);
                        }
                    }
                });

                _logger.LogInformation("Successfully loaded {RowCount} rows from dictionary list", _rows.Count);
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.LoadData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from dictionary list");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        public async Task<DataTable> ExportDataAsync()
        {
            try
            {
                _logger.LogDebug("Exporting data to DataTable");

                return await Task.Run(() =>
                {
                    var dataTable = new DataTable();

                    foreach (var column in _columns.Where(c => !c.IsSpecialColumn))
                    {
                        dataTable.Columns.Add(column.Name, column.DataType);
                    }

                    foreach (var row in _rows.Where(r => !r.IsEmpty))
                    {
                        var dataRow = dataTable.NewRow();
                        foreach (var column in _columns.Where(c => !c.IsSpecialColumn))
                        {
                            dataRow[column.Name] = row.GetValue<object>(column.Name) ?? DBNull.Value;
                        }
                        dataTable.Rows.Add(dataRow);
                    }

                    _logger.LogInformation("Exported {RowCount} rows to DataTable", dataTable.Rows.Count);
                    return dataTable;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
                return new DataTable();
            }
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                _logger.LogInformation("Clearing all data");

                await Task.Run(() =>
                {
                    foreach (var row in _rows)
                    {
                        foreach (var cell in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                        {
                            cell.Value = null;
                            cell.SetValidationErrors(new List<string>());
                        }
                    }
                });

                _logger.LogInformation("Successfully cleared all data");
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.ClearData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            try
            {
                _logger.LogDebug("Validating all rows");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating all rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        public async Task RemoveRowsByConditionAsync(string columnName, Func<object, bool> condition)
        {
            try
            {
                _logger.LogDebug("Removing rows by condition for column: {ColumnName}", columnName);

                await Task.Run(() =>
                {
                    var rowsToRemove = new List<DataGridRowModel>();

                    foreach (var row in _rows)
                    {
                        var cell = row.GetCell(columnName);
                        if (cell != null && condition(cell.Value))
                        {
                            rowsToRemove.Add(row);
                        }
                    }

                    foreach (var row in rowsToRemove)
                    {
                        _rows.Remove(row);
                    }

                    _rows = _rows.OrderBy(r => r.IsEmpty).ToList();
                });

                _logger.LogInformation("Removed rows by condition for column: {ColumnName}", columnName);
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.RemoveRows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing rows by condition for column: {ColumnName}", columnName);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
            }
        }

        public async Task RemoveEmptyRowsAsync()
        {
            try
            {
                _logger.LogDebug("Removing empty rows");

                await Task.Run(() =>
                {
                    var dataRows = _rows.Where(r => !r.IsEmpty).ToList();
                    var emptyRowsCount = _rows.Count - dataRows.Count;

                    _rows = dataRows;

                    for (int i = 0; i < emptyRowsCount; i++)
                    {
                        var emptyRow = CreateEmptyRow();
                        _rows.Add(emptyRow);
                    }
                });

                _logger.LogInformation("Removed empty rows");
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.RemoveEmptyRows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        private DataGridRowModel CreateEmptyRow()
        {
            var row = new DataGridRowModel();
            foreach (var column in _columns)
            {
                var cell = new DataGridCellModel
                {
                    ColumnName = column.Name,
                    DataType = column.DataType,
                    Value = null
                };
                row.AddCell(column.Name, cell);
            }
            return row;
        }

        private bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        protected virtual void OnDataChanged(DataChangeEventArgs e)
        {
            DataChanged?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}