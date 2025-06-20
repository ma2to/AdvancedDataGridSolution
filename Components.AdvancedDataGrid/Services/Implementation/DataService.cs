// ===========================================
// Services/Implementation/DataService.cs
// ===========================================
using Components.AdvancedDataGrid.Events;
using Components.AdvancedDataGrid.Helpers;
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Components.AdvancedDataGrid.Services.Implementation
{
    public class DataService : IDataService
    {
        private List<DataGridRowModel> _rows = new();
        private List<ColumnDefinitionModel> _columns = new();

        // OPRAVENÉ - používa DataChangeEventArgs namiesto DataChangedEventArgs
        public event EventHandler<DataChangeEventArgs>? DataChanged;
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        public void Initialize(List<ColumnDefinitionModel> columns)
        {
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                await Task.Run(() =>
                {
                    _rows.Clear();

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
                });

                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.LoadData });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        public async Task LoadDataAsync(List<Dictionary<string, object>> data)
        {
            try
            {
                await Task.Run(() =>
                {
                    _rows.Clear();

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
                });

                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.LoadData });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        public async Task<DataTable> ExportDataAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    var dataTable = new DataTable();

                    // Vytvor stĺpce
                    foreach (var column in _columns.Where(c => !c.IsSpecialColumn))
                    {
                        dataTable.Columns.Add(column.Name, column.DataType);
                    }

                    // Pridaj riadky
                    foreach (var row in _rows.Where(r => !r.IsEmpty))
                    {
                        var dataRow = dataTable.NewRow();
                        foreach (var column in _columns.Where(c => !c.IsSpecialColumn))
                        {
                            dataRow[column.Name] = row.GetValue<object>(column.Name) ?? DBNull.Value;
                        }
                        dataTable.Rows.Add(dataRow);
                    }

                    return dataTable;
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
                return new DataTable();
            }
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
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

                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.ClearData });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            try
            {
                // This will be handled by ValidationService
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        public async Task RemoveRowsByConditionAsync(string columnName, Func<object, bool> condition)
        {
            try
            {
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

                    // Zoradi riadky - prázdne na koniec
                    _rows = _rows.OrderBy(r => r.IsEmpty).ToList();
                });

                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.RemoveRows });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
            }
        }

        public async Task RemoveEmptyRowsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var dataRows = _rows.Where(r => !r.IsEmpty).ToList();
                    var emptyRowsCount = _rows.Count - dataRows.Count;

                    _rows = dataRows;

                    // Pridaj prázdne riadky na koniec
                    for (int i = 0; i < emptyRowsCount; i++)
                    {
                        var emptyRow = CreateEmptyRow();
                        _rows.Add(emptyRow);
                    }
                });

                OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.RemoveEmptyRows });
            }
            catch (Exception ex)
            {
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