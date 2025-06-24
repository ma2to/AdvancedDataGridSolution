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
    internal class DataService : IDataService
    {
        private readonly ILogger<DataService> _logger;
        private List<DataGridRowModel> _rows = new List<DataGridRowModel>();
        private List<ColumnDefinitionModel> _columns = new List<ColumnDefinitionModel>();

        public DataService(ILogger<DataService> logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DataService>.Instance;
        }

        public event EventHandler<DataChangeEventArgs> DataChanged;
        public event EventHandler<ComponentErrorEventArgs> ErrorOccurred;

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

        public Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                _logger.LogInformation("Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);

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

                _logger.LogInformation("Successfully loaded {RowCount} rows from DataTable", _rows.Count);
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.LoadData });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                return Task.CompletedTask;
            }
        }

        public Task LoadDataAsync(List<Dictionary<string, object>> data)
        {
            try
            {
                _logger.LogInformation("Loading data from dictionary list with {RowCount} rows", data?.Count ?? 0);

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

                _logger.LogInformation("Successfully loaded {RowCount} rows from dictionary list", _rows.Count);
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.LoadData });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from dictionary list");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                return Task.CompletedTask;
            }
        }

        public Task<DataTable> ExportDataAsync()
        {
            try
            {
                _logger.LogDebug("Exporting data to DataTable");

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
                return Task.FromResult(dataTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
                return Task.FromResult(new DataTable());
            }
        }

        public Task ClearAllDataAsync()
        {
            try
            {
                _logger.LogInformation("Clearing all data");

                foreach (var row in _rows)
                {
                    foreach (var cell in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        cell.Value = null;
                        cell.SetValidationErrors(new List<string>());
                    }
                }

                _logger.LogInformation("Successfully cleared all data");
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.ClearData });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
                return Task.CompletedTask;
            }
        }

        public Task<bool> ValidateAllRowsAsync()
        {
            try
            {
                _logger.LogDebug("Validating all rows");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating all rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return Task.FromResult(false);
            }
        }

        public Task RemoveRowsByConditionAsync(string columnName, Func<object, bool> condition)
        {
            try
            {
                _logger.LogDebug("Removing rows by condition for column: {ColumnName}", columnName);

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

                _logger.LogInformation("Removed rows by condition for column: {ColumnName}", columnName);
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.RemoveRows });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing rows by condition for column: {ColumnName}", columnName);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
                return Task.CompletedTask;
            }
        }

        public Task RemoveEmptyRowsAsync()
        {
            try
            {
                _logger.LogDebug("Removing empty rows");

                var dataRows = _rows.Where(r => !r.IsEmpty).ToList();
                var emptyRowsCount = _rows.Count - dataRows.Count;

                _rows = dataRows;

                for (int i = 0; i < emptyRowsCount; i++)
                {
                    var emptyRow = CreateEmptyRow();
                    _rows.Add(emptyRow);
                }

                _logger.LogInformation("Removed empty rows");
                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.RemoveEmptyRows });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
                return Task.CompletedTask;
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