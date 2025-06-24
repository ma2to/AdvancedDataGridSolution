// RpaWpfComponents/AdvancedDataGrid/ViewModels/AdvancedDataGridViewModel.cs
using RpaWpfComponents.AdvancedDataGrid.ViewModels;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.AdvancedDataGrid.Collections;
using RpaWpfComponents.AdvancedDataGrid.Commands;
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RpaWpfComponents.AdvancedDataGrid.ViewModels
{
    public class AdvancedDataGridViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private readonly IValidationService _validationService;
        private readonly IClipboardService _clipboardService;
        private readonly IColumnService _columnService;
        private readonly IExportService _exportService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<AdvancedDataGridViewModel> _logger;

        private ObservableRangeCollection<DataGridRowModel> _rows = new();
        private ObservableRangeCollection<ColumnDefinitionViewModel> _columns = new();
        private bool _isValidating = false;
        private double _validationProgress = 0;
        private string _validationStatus = "Pripravené";
        private bool _isInitialized = false;

        public AdvancedDataGridViewModel(
            IDataService dataService,
            IValidationService validationService,
            IClipboardService clipboardService,
            IColumnService columnService,
            IExportService exportService,
            INavigationService navigationService,
            ILogger<AdvancedDataGridViewModel>? logger = null)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _columnService = columnService ?? throw new ArgumentNullException(nameof(columnService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdvancedDataGridViewModel>.Instance;

            InitializeCollections();
            InitializeCommands();
            SubscribeToEvents();

            _logger.LogDebug("AdvancedDataGridViewModel created");
        }

        #region Properties

        public ObservableRangeCollection<DataGridRowModel> Rows
        {
            get => _rows;
            set => SetProperty(ref _rows, value);
        }

        public ObservableRangeCollection<ColumnDefinitionViewModel> Columns
        {
            get => _columns;
            set => SetProperty(ref _columns, value);
        }

        public bool IsValidating
        {
            get => _isValidating;
            set => SetProperty(ref _isValidating, value);
        }

        public double ValidationProgress
        {
            get => _validationProgress;
            set => SetProperty(ref _validationProgress, value);
        }

        public string ValidationStatus
        {
            get => _validationStatus;
            set => SetProperty(ref _validationStatus, value);
        }

        public INavigationService NavigationService => _navigationService;
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Commands

        public ICommand ValidateAllCommand { get; private set; } = null!;
        public ICommand ClearAllDataCommand { get; private set; } = null!;
        public ICommand RemoveEmptyRowsCommand { get; private set; } = null!;
        public ICommand CopyCommand { get; private set; } = null!;
        public ICommand PasteCommand { get; private set; } = null!;
        public ICommand DeleteRowCommand { get; private set; } = null!;
        public ICommand ExportToDataTableCommand { get; private set; } = null!;

        #endregion

        #region Public Methods

        public async Task InitializeAsync(List<ColumnDefinitionModel> columnDefinitions, List<ValidationRuleModel>? validationRules = null)
        {
            try
            {
                if (_isInitialized)
                {
                    _logger.LogWarning("Component already initialized. Call Reset() first if needed.");
                    return;
                }

                _logger.LogInformation("Initializing AdvancedDataGrid with {ColumnCount} columns and {RuleCount} validation rules",
                    columnDefinitions?.Count ?? 0, validationRules?.Count ?? 0);

                var processedColumns = _columnService.ProcessColumnDefinitions(columnDefinitions ?? new List<ColumnDefinitionModel>());
                _dataService.Initialize(processedColumns);

                var columnVMs = processedColumns.Select(c => new ColumnDefinitionViewModel(c)).ToList();
                Columns.Clear();
                Columns.AddRange(columnVMs);

                if (validationRules != null)
                {
                    foreach (var rule in validationRules)
                    {
                        _validationService.AddValidationRule(rule);
                    }
                    _logger.LogDebug("Added {RuleCount} validation rules", validationRules.Count);
                }

                await CreateInitialRowsAsync();
                _navigationService.Initialize(Rows.ToList(), processedColumns);

                _isInitialized = true;
                _logger.LogInformation("AdvancedDataGrid initialization completed: {RowCount} rows, {ColumnCount} columns",
                    Rows.Count, Columns.Count);
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                _logger.LogError(ex, "Error during initialization");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
            }
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Component must be initialized first!");

                _logger.LogInformation("Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);

                IsValidating = true;
                ValidationStatus = "Načítavajú sa dáta...";
                ValidationProgress = 0;

                Rows.Clear();

                var newRows = new List<DataGridRowModel>();
                var rowIndex = 0;
                var totalRows = dataTable?.Rows.Count ?? 0;

                if (dataTable != null)
                {
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        var gridRow = CreateRowForLoading();

                        _logger.LogTrace("Loading row {RowIndex}/{TotalRows}", rowIndex + 1, totalRows);

                        foreach (var column in Columns.Where(c => !c.IsSpecialColumn))
                        {
                            if (dataTable.Columns.Contains(column.Name))
                            {
                                var value = dataRow[column.Name];
                                var cell = gridRow.GetCell(column.Name);
                                if (cell != null)
                                {
                                    cell.SetValueWithoutValidation(value == DBNull.Value ? null : value);
                                }
                            }
                        }

                        await ValidateRowAfterLoading(gridRow);

                        newRows.Add(gridRow);
                        rowIndex++;
                        ValidationProgress = (double)rowIndex / totalRows * 90;
                    }
                }

                while (newRows.Count < 50)
                {
                    newRows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                Rows.AddRange(newRows);

                ValidationStatus = "Validácia dokončená";
                ValidationProgress = 100;

                var validRows = newRows.Count(r => !r.IsEmpty && !r.HasValidationErrors);
                var invalidRows = newRows.Count(r => !r.IsEmpty && r.HasValidationErrors);

                _logger.LogInformation("Data loaded: {TotalRows} rows, {ValidRows} valid, {InvalidRows} invalid",
                    totalRows, validRows, invalidRows);

                await Task.Delay(2000);
                IsValidating = false;
                ValidationStatus = "Pripravené";
            }
            catch (Exception ex)
            {
                IsValidating = false;
                ValidationStatus = "Chyba pri načítavaní";
                _logger.LogError(ex, "Error loading data from DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        public async Task LoadDataAsync(List<Dictionary<string, object>> data)
        {
            try
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Component must be initialized first!");

                var dataTable = ConvertToDataTable(data);
                await LoadDataAsync(dataTable);
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
                var columnDefinitions = Columns.Select(c => c.Model).ToList();
                var result = await _exportService.ExportToDataTableAsync(Rows.ToList(), columnDefinitions);
                _logger.LogInformation("Exported {RowCount} rows to DataTable", result.Rows.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
                return new DataTable();
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            try
            {
                _logger.LogDebug("Starting validation of all rows");
                IsValidating = true;
                ValidationProgress = 0;
                ValidationStatus = "Validujú sa riadky...";

                var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
                var results = await _validationService.ValidateAllRowsAsync(dataRows);

                var allValid = results.All(r => r.IsValid);
                ValidationStatus = allValid ? "Všetky riadky sú validné" : "Nájdené validačné chyby";

                _logger.LogInformation("Validation completed: all valid = {AllValid}", allValid);

                await Task.Delay(2000);
                ValidationStatus = "Pripravené";
                IsValidating = false;

                return allValid;
            }
            catch (Exception ex)
            {
                IsValidating = false;
                ValidationStatus = "Chyba pri validácii";
                _logger.LogError(ex, "Error validating all rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        public async Task<List<ValidationResultModel>> ValidateRowAsync(DataGridRowModel row)
        {
            try
            {
                return await _validationService.ValidateRowAsync(row);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating row");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateRowAsync"));
                return new List<ValidationResultModel>();
            }
        }

        public void Reset()
        {
            try
            {
                _logger.LogInformation("Resetting component");

                Rows.Clear();
                Columns.Clear();
                _validationService.ClearValidationRules();
                _isInitialized = false;

                IsValidating = false;
                ValidationProgress = 0;
                ValidationStatus = "Pripravené";

                _logger.LogInformation("Component reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reset");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
            }
        }

        public async Task<int> RemoveRowsByCustomValidationAsync(List<ValidationRuleModel> customValidationRules)
        {
            try
            {
                if (!_isInitialized || customValidationRules?.Count == 0)
                    return 0;

                _logger.LogDebug("Removing rows by custom validation with {RuleCount} rules", customValidationRules.Count);

                var rowsToRemove = new List<DataGridRowModel>();
                var dataRows = Rows.Where(r => !r.IsEmpty).ToList();

                foreach (var row in dataRows)
                {
                    foreach (var rule in customValidationRules)
                    {
                        var cell = row.GetCell(rule.ColumnName);
                        if (cell != null && !rule.Validate(cell.Value, row))
                        {
                            rowsToRemove.Add(row);
                            break;
                        }
                    }
                }

                foreach (var row in rowsToRemove)
                {
                    Rows.Remove(row);
                }

                while (Rows.Count < 50)
                {
                    Rows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                _logger.LogInformation("Removed {RowCount} rows by custom validation", rowsToRemove.Count);
                return rowsToRemove.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing rows by custom validation");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByCustomValidationAsync"));
                return 0;
            }
        }

        public async Task RemoveRowsByConditionAsync(string columnName, Func<object, bool> condition)
        {
            try
            {
                _logger.LogDebug("Removing rows by condition for column: {ColumnName}", columnName);

                var rowsToRemove = new List<DataGridRowModel>();

                foreach (var row in Rows.Where(r => !r.IsEmpty).ToList())
                {
                    if (columnName == "HasValidationErrors")
                    {
                        if (condition(row.HasValidationErrors))
                            rowsToRemove.Add(row);
                    }
                    else
                    {
                        var cell = row.GetCell(columnName);
                        if (cell != null && condition(cell.Value))
                            rowsToRemove.Add(row);
                    }
                }

                foreach (var row in rowsToRemove)
                {
                    Rows.Remove(row);
                }

                while (Rows.Count < 50)
                {
                    Rows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                _logger.LogInformation("Removed {RowCount} rows by condition for column: {ColumnName}", rowsToRemove.Count, columnName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing rows by condition for column: {ColumnName}", columnName);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
            }
        }

        #endregion

        #region Private Methods

        private void InitializeCollections()
        {
            Rows = new ObservableRangeCollection<DataGridRowModel>();
            Columns = new ObservableRangeCollection<ColumnDefinitionViewModel>();
        }

        private void InitializeCommands()
        {
            ValidateAllCommand = new AsyncRelayCommand(async () => await ValidateAllRowsAsync());
            ClearAllDataCommand = new AsyncRelayCommand(async () => await ClearAllDataAsync());
            RemoveEmptyRowsCommand = new AsyncRelayCommand(async () => await RemoveEmptyRowsAsync());
            CopyCommand = new AsyncRelayCommand(CopySelectedCellsAsync);
            PasteCommand = new AsyncRelayCommand(PasteFromClipboardAsync);
            DeleteRowCommand = new RelayCommand<DataGridRowModel>(DeleteRow);
            ExportToDataTableCommand = new AsyncRelayCommand<object>(async _ => await ExportDataAsync());
        }

        private void SubscribeToEvents()
        {
            _dataService.DataChanged += OnDataChanged;
            _dataService.ErrorOccurred += OnDataServiceErrorOccurred;
            _validationService.ValidationCompleted += OnValidationCompleted;
            _validationService.ValidationErrorOccurred += OnValidationServiceErrorOccurred;
            _navigationService.ErrorOccurred += OnNavigationServiceErrorOccurred;
        }

        private async Task CreateInitialRowsAsync()
        {
            var rows = new List<DataGridRowModel>();

            for (int i = 0; i < 50; i++)
            {
                var row = CreateEmptyRowWithRealTimeValidation();
                rows.Add(row);
            }

            Rows.Clear();
            Rows.AddRange(rows);
        }

        private DataGridRowModel CreateRowForLoading()
        {
            var row = new DataGridRowModel();

            foreach (var column in Columns)
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

        private DataGridRowModel CreateEmptyRowWithRealTimeValidation()
        {
            var row = new DataGridRowModel();

            foreach (var column in Columns)
            {
                var cell = new DataGridCellModel
                {
                    ColumnName = column.Name,
                    DataType = column.DataType,
                    Value = null
                };

                cell.ValueChanged += async (s, e) => await OnCellValueChangedRealTime(row, cell);
                row.AddCell(column.Name, cell);
            }

            return row;
        }

        private async Task ValidateRowAfterLoading(DataGridRowModel row)
        {
            try
            {
                row.UpdateEmptyStatusAfterLoading();

                if (!row.IsEmpty)
                {
                    foreach (var cell in row.Cells.Values.Where(c => !_columnService.IsSpecialColumn(c.ColumnName)))
                    {
                        await _validationService.ValidateCellAsync(cell, row);
                    }

                    row.UpdateValidationStatus();
                }

                foreach (var cell in row.Cells.Values.Where(c => !_columnService.IsSpecialColumn(c.ColumnName)))
                {
                    cell.ValueChanged += async (s, e) => await OnCellValueChangedRealTime(row, cell);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating row after loading");
            }
        }

        private async Task OnCellValueChangedRealTime(DataGridRowModel row, DataGridCellModel cell)
        {
            try
            {
                if (row.IsEmpty)
                {
                    cell.SetValidationErrors(new List<string>());
                    row.UpdateValidationStatus();
                    return;
                }

                await _validationService.ValidateCellAsync(cell, row);
                row.UpdateValidationStatus();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnCellValueChangedRealTime"));
            }
        }

        private async Task ClearAllDataAsync()
        {
            try
            {
                if (!_isInitialized) return;

                _logger.LogDebug("Clearing all data");

                foreach (var row in Rows)
                {
                    foreach (var cell in row.Cells.Values.Where(c => !_columnService.IsSpecialColumn(c.ColumnName)))
                    {
                        cell.Value = null;
                        cell.SetValidationErrors(new List<string>());
                    }
                }

                _logger.LogInformation("All data cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
            }
        }

        private async Task RemoveEmptyRowsAsync()
        {
            try
            {
                _logger.LogDebug("Removing empty rows");

                var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
                var emptyRowsNeeded = Math.Max(0, 50 - dataRows.Count);

                Rows.Clear();
                Rows.AddRange(dataRows);

                for (int i = 0; i < emptyRowsNeeded; i++)
                {
                    Rows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                _logger.LogInformation("Empty rows removed, {DataRowCount} data rows kept", dataRows.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        private async Task CopySelectedCellsAsync()
        {
            try
            {
                var currentCell = _navigationService.CurrentCell;
                if (currentCell != null)
                {
                    var data = currentCell.Value?.ToString() ?? "";
                    await _clipboardService.SetClipboardDataAsync(data);
                    _logger.LogDebug("Copied cell data to clipboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying selected cells");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "CopySelectedCellsAsync"));
            }
        }

        private async Task PasteFromClipboardAsync()
        {
            try
            {
                if (!_isInitialized) return;

                var clipboardData = await _clipboardService.GetClipboardDataAsync();
                if (string.IsNullOrEmpty(clipboardData)) return;

                var parsedData = _clipboardService.ParseFromExcelFormat(clipboardData);
                var startRowIndex = _navigationService.CurrentRowIndex;
                var startColumnIndex = _navigationService.CurrentColumnIndex;

                if (startRowIndex >= 0 && startColumnIndex >= 0)
                {
                    var editableColumns = Columns.Where(c => !c.IsSpecialColumn).ToList();
                    if (startColumnIndex < editableColumns.Count)
                    {
                        await PasteDataAtPositionAsync(parsedData, startRowIndex, startColumnIndex);
                        _logger.LogDebug("Pasted data from clipboard at position [{Row},{Col}]", startRowIndex, startColumnIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pasting from clipboard");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "PasteFromClipboardAsync"));
            }
        }

        private async Task PasteDataAtPositionAsync(string[,] data, int startRowIndex, int startColumnIndex)
        {
            try
            {
                int dataRows = data.GetLength(0);
                int dataCols = data.GetLength(1);
                var editableColumns = Columns.Where(c => !c.IsSpecialColumn).ToList();

                for (int i = 0; i < dataRows; i++)
                {
                    int targetRowIndex = startRowIndex + i;

                    while (targetRowIndex >= Rows.Count)
                    {
                        Rows.Add(CreateEmptyRowWithRealTimeValidation());
                    }

                    for (int j = 0; j < dataCols; j++)
                    {
                        int targetColumnIndex = startColumnIndex + j;
                        if (targetColumnIndex >= editableColumns.Count) break;

                        var columnName = editableColumns[targetColumnIndex].Name;
                        var targetRow = Rows[targetRowIndex];

                        if (!_columnService.IsSpecialColumn(columnName) &&
                            targetRow.Cells.ContainsKey(columnName))
                        {
                            targetRow.SetValue(columnName, data[i, j]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pasting data at position");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "PasteDataAtPositionAsync"));
            }
        }

        private void DeleteRow(DataGridRowModel? row)
        {
            try
            {
                if (row != null && Rows.Contains(row))
                {
                    foreach (var cell in row.Cells.Values.Where(c => !_columnService.IsSpecialColumn(c.ColumnName)))
                    {
                        cell.Value = null;
                        cell.SetValidationErrors(new List<string>());
                    }

                    var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
                    var emptyRows = Rows.Where(r => r.IsEmpty).ToList();

                    Rows.Clear();
                    Rows.AddRange(dataRows);
                    Rows.AddRange(emptyRows);

                    _logger.LogDebug("Row deleted");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting row");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "DeleteRow"));
            }
        }

        private DataTable ConvertToDataTable(List<Dictionary<string, object>> data)
        {
            var dataTable = new DataTable();

            if (data?.Count > 0)
            {
                foreach (var key in data[0].Keys)
                {
                    dataTable.Columns.Add(key, typeof(object));
                }

                foreach (var row in data)
                {
                    var dataRow = dataTable.NewRow();
                    foreach (var kvp in row)
                    {
                        dataRow[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }

        #endregion

        #region Event Handlers

        private void OnDataChanged(object? sender, DataChangeEventArgs e)
        {
            _logger.LogTrace("Data changed: {ChangeType}", e.ChangeType);
        }

        private void OnValidationCompleted(object? sender, ValidationCompletedEventArgs e)
        {
            _logger.LogTrace("Validation completed for row. Is valid: {IsValid}", e.IsValid);
        }

        private void OnDataServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "DataService error: {Operation}", e.Operation);
            OnErrorOccurred(e);
        }

        private void OnValidationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "ValidationService error: {Operation}", e.Operation);
            OnErrorOccurred(e);
        }

        private void OnNavigationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "NavigationService error: {Operation}", e.Operation);
            OnErrorOccurred(e);
        }

        #endregion

        #region Events & Property Changed

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}