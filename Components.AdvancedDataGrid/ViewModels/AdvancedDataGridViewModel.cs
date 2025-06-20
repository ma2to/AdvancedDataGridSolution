// ViewModels/AdvancedDataGridViewModel.cs - OPRAVENÝ
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Services.Interfaces;
using Components.AdvancedDataGrid.Commands;
using Components.AdvancedDataGrid.Events;
using Components.AdvancedDataGrid.Collections;

namespace Components.AdvancedDataGrid.ViewModels
{
    public class AdvancedDataGridViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private readonly IValidationService _validationService;
        private readonly IClipboardService _clipboardService;
        private readonly IColumnService _columnService;
        private readonly IExportService _exportService;
        private readonly INavigationService _navigationService;

        private ObservableRangeCollection<DataGridRowModel> _rows = new();
        private ObservableRangeCollection<ColumnDefinitionViewModel> _columns = new();
        private MirrorEditorViewModel? _mirrorEditor;
        private bool _showMirrorEditor = true;
        private bool _isValidating = false;
        private double _validationProgress = 0;
        private string _validationStatus = "Pripravené";

        public AdvancedDataGridViewModel(
            IDataService dataService,
            IValidationService validationService,
            IClipboardService clipboardService,
            IColumnService columnService,
            IExportService exportService,
            INavigationService navigationService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _columnService = columnService ?? throw new ArgumentNullException(nameof(columnService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            InitializeCollections();
            InitializeCommands();
            SubscribeToEvents();
        }

        // Properties
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

        public MirrorEditorViewModel? MirrorEditor
        {
            get => _mirrorEditor;
            set => SetProperty(ref _mirrorEditor, value);
        }

        public bool ShowMirrorEditor
        {
            get => _showMirrorEditor;
            set => SetProperty(ref _showMirrorEditor, value);
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

        // Navigation Service property pre bindings
        public INavigationService NavigationService => _navigationService;

        // Commands - OPRAVENÉ nullability
        public ICommand ValidateAllCommand { get; private set; } = null!;
        public ICommand ClearAllDataCommand { get; private set; } = null!;
        public ICommand RemoveEmptyRowsCommand { get; private set; } = null!;
        public ICommand CopyCommand { get; private set; } = null!;
        public ICommand PasteCommand { get; private set; } = null!;
        public ICommand DeleteRowCommand { get; private set; } = null!;
        public ICommand ExportToDataTableCommand { get; private set; } = null!;

        // Public Methods
        public async Task InitializeAsync(List<ColumnDefinitionModel> columnDefinitions, List<ValidationRuleModel>? validationRules = null)
        {
            try
            {
                // Spracuj stĺpce
                var processedColumns = _columnService.ProcessColumnDefinitions(columnDefinitions);

                // Inicializuj DataService
                _dataService.Initialize(processedColumns);

                // Vytvor ViewModely pre stĺpce
                var columnVMs = processedColumns.Select(c => new ColumnDefinitionViewModel(c)).ToList();
                Columns.Clear();
                Columns.AddRange(columnVMs);

                // Nastav validačné pravidlá
                if (validationRules != null)
                {
                    foreach (var rule in validationRules)
                    {
                        _validationService.AddValidationRule(rule);
                    }
                }

                // Vytvor počiatočné riadky
                await CreateInitialRowsAsync();

                // Inicializuj navigation service s delay aby sa DataGrid stihol vytvoriť
                await Task.Delay(100);
                _navigationService.Initialize(Rows.ToList(), processedColumns);

                // Inicializuj mirror editor
                MirrorEditor = new MirrorEditorViewModel(_navigationService);

                System.Diagnostics.Debug.WriteLine($"Initialized with {Rows.Count} rows, {Columns.Count} columns, {validationRules?.Count ?? 0} validation rules");
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
            }
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                IsValidating = true;
                ValidationStatus = "Načítavajú sa dáta...";
                ValidationProgress = 0;

                // Clear existing rows
                Rows.Clear();

                // Load data and convert to DataGridRowModel
                var newRows = new List<DataGridRowModel>();
                var rowIndex = 0;
                var totalRows = dataTable.Rows.Count;

                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var gridRow = new DataGridRowModel();

                    foreach (var column in Columns)
                    {
                        var cell = new DataGridCellModel
                        {
                            ColumnName = column.Name,
                            DataType = column.DataType
                        };

                        if (dataTable.Columns.Contains(column.Name) && !column.IsSpecialColumn)
                        {
                            var value = dataRow[column.Name];
                            cell.Value = value == DBNull.Value ? null : value;
                        }

                        gridRow.AddCell(column.Name, cell);

                        // Subscribe to cell value changes for auto-validation
                        cell.ValueChanged += async (s, e) => await OnCellValueChanged(gridRow, cell);
                    }

                    newRows.Add(gridRow);

                    // Update progress
                    rowIndex++;
                    ValidationProgress = (double)rowIndex / totalRows * 50; // 50% for loading
                }

                // Add remaining empty rows to reach minimum count
                var minimumRows = 50;
                while (newRows.Count < minimumRows)
                {
                    newRows.Add(CreateEmptyRow());
                }

                // Add all rows to collection
                Rows.AddRange(newRows);

                ValidationStatus = "Spúšťa sa validácia...";
                ValidationProgress = 50;

                // Validate all loaded data
                await ValidateAllRowsAsync();

                ValidationStatus = "Hotovo";
                ValidationProgress = 100;

                await Task.Delay(2000); // Show status for 2 seconds
                IsValidating = false;
                ValidationStatus = "Pripravené";
            }
            catch (Exception ex)
            {
                IsValidating = false;
                ValidationStatus = "Chyba pri načítavaní";
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        public async Task<DataTable> ExportDataAsync()
        {
            try
            {
                var columnDefinitions = Columns.Select(c => c.Model).ToList();
                return await _exportService.ExportToDataTableAsync(Rows.ToList(), columnDefinitions);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
                return new DataTable();
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            try
            {
                IsValidating = true;
                ValidationProgress = 0;
                ValidationStatus = "Validujú sa riadky...";

                var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
                var totalRows = dataRows.Count;
                var processedRows = 0;

                var allValid = true;

                foreach (var row in dataRows)
                {
                    var results = await _validationService.ValidateRowAsync(row);
                    if (results.Any(r => !r.IsValid))
                    {
                        allValid = false;
                    }

                    processedRows++;
                    ValidationProgress = (double)processedRows / totalRows * 100;
                }

                ValidationStatus = allValid ? "Všetky riadky sú validné" : "Nájdené validačné chyby";

                await Task.Delay(2000); // Show status for 2 seconds
                ValidationStatus = "Pripravené";
                IsValidating = false;

                return allValid;
            }
            catch (Exception ex)
            {
                IsValidating = false;
                ValidationStatus = "Chyba pri validácii";
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
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateRowAsync"));
                return new List<ValidationResultModel>();
            }
        }

        public async Task RemoveRowsByConditionAsync(string columnName, Func<object, bool> condition)
        {
            try
            {
                if (columnName == "HasValidationErrors")
                {
                    // Špeciálny handling pre HasValidationErrors
                    var rowsToRemove = new List<DataGridRowModel>();

                    foreach (var row in Rows.Where(r => !r.IsEmpty).ToList())
                    {
                        if (condition(row.HasValidationErrors))
                        {
                            rowsToRemove.Add(row);
                        }
                    }

                    foreach (var row in rowsToRemove)
                    {
                        Rows.Remove(row);
                    }

                    // Pridaj prázdne riadky na koniec
                    while (Rows.Count < 50)
                    {
                        Rows.Add(CreateEmptyRow());
                    }

                    System.Diagnostics.Debug.WriteLine($"Removed {rowsToRemove.Count} invalid rows");
                }
                else
                {
                    await _dataService.RemoveRowsByConditionAsync(columnName, condition);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
            }
        }

        // Private Methods
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
            _navigationService.CellChanged += OnCellChanged;
            _navigationService.ErrorOccurred += OnNavigationServiceErrorOccurred;
        }

        private async Task CreateInitialRowsAsync()
        {
            var rows = new List<DataGridRowModel>();

            for (int i = 0; i < 50; i++) // 50 počiatočných riadkov
            {
                var row = CreateEmptyRow();
                rows.Add(row);
            }

            Rows.Clear();
            Rows.AddRange(rows);
        }

        private DataGridRowModel CreateEmptyRow()
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

                // Subscribe to cell value changes for auto-validation
                cell.ValueChanged += async (s, e) => await OnCellValueChanged(row, cell);

                row.AddCell(column.Name, cell);
            }

            return row;
        }

        private async Task OnCellValueChanged(DataGridRowModel row, DataGridCellModel cell)
        {
            try
            {
                // Auto-validate cell when value changes
                await _validationService.ValidateCellAsync(cell, row);

                // Update row validation status
                row.UpdateValidationStatus();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnCellValueChanged"));
            }
        }

        private async Task ClearAllDataAsync()
        {
            try
            {
                foreach (var row in Rows)
                {
                    foreach (var cell in row.Cells.Values.Where(c => !_columnService.IsSpecialColumn(c.ColumnName)))
                    {
                        cell.Value = null;
                        cell.SetValidationErrors(new List<string>());
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
            }
        }

        private async Task RemoveEmptyRowsAsync()
        {
            try
            {
                var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
                var emptyRowsNeeded = Math.Max(0, 50 - dataRows.Count);

                Rows.Clear();
                Rows.AddRange(dataRows);

                // Add empty rows to maintain minimum count
                for (int i = 0; i < emptyRowsNeeded; i++)
                {
                    Rows.Add(CreateEmptyRow());
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        private async Task CopySelectedCellsAsync()
        {
            try
            {
                // Získaj aktuálnu bunku z navigation service
                var currentCell = _navigationService.CurrentCell;
                if (currentCell != null)
                {
                    var data = currentCell.Value?.ToString() ?? "";
                    await _clipboardService.SetClipboardDataAsync(data);
                    System.Diagnostics.Debug.WriteLine($"Copied to clipboard: '{data}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No current cell to copy");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "CopySelectedCellsAsync"));
            }
        }

        private async Task PasteFromClipboardAsync()
        {
            try
            {
                var clipboardData = await _clipboardService.GetClipboardDataAsync();
                if (string.IsNullOrEmpty(clipboardData))
                    return;

                var parsedData = _clipboardService.ParseFromExcelFormat(clipboardData);

                // Aplikuj dáta začínajúc od aktuálnej bunky
                var startRowIndex = _navigationService.CurrentRowIndex;
                var startColumnIndex = _navigationService.CurrentColumnIndex;

                if (startRowIndex >= 0 && startColumnIndex >= 0)
                {
                    await PasteDataAtPositionAsync(parsedData, startRowIndex, startColumnIndex);
                }
            }
            catch (Exception ex)
            {
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

                    // Pridaj nové riadky ak je potreba
                    while (targetRowIndex >= Rows.Count)
                    {
                        Rows.Add(CreateEmptyRow());
                    }

                    for (int j = 0; j < dataCols; j++)
                    {
                        int targetColumnIndex = startColumnIndex + j;

                        if (targetColumnIndex >= editableColumns.Count)
                            break; // Presahuje počet stĺpcov

                        var columnName = editableColumns[targetColumnIndex].Name;
                        var targetRow = Rows[targetRowIndex];

                        if (targetRow.Cells.ContainsKey(columnName))
                        {
                            targetRow.SetValue(columnName, data[i, j]);
                        }
                    }
                }

                // Validuj vložené dáta
                await ValidateAllRowsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "PasteDataAtPositionAsync"));
            }
        }

        private void DeleteRow(DataGridRowModel? row)
        {
            try
            {
                if (row != null && Rows.Contains(row))
                {
                    // Vymaž obsah riadku
                    foreach (var cell in row.Cells.Values.Where(c => !_columnService.IsSpecialColumn(c.ColumnName)))
                    {
                        cell.Value = null;
                        cell.SetValidationErrors(new List<string>());
                    }

                    // Zoradi riadky - prázdne na koniec
                    var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
                    var emptyRows = Rows.Where(r => r.IsEmpty).ToList();

                    Rows.Clear();
                    Rows.AddRange(dataRows);
                    Rows.AddRange(emptyRows);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "DeleteRow"));
            }
        }

        // Event Handlers
        private void OnDataChanged(object? sender, DataChangeEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Data changed: {e.ChangeType}");
        }

        private void OnValidationCompleted(object? sender, ValidationCompletedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Validation completed for row. Is valid: {e.IsValid}");
        }

        private void OnCellChanged(object? sender, CellNavigationEventArgs e)
        {
            // Update mirror editor
            MirrorEditor?.SetCurrentCell(e.NewCell);
            System.Diagnostics.Debug.WriteLine($"Cell changed from [{e.OldRowIndex},{e.OldColumnIndex}] to [{e.NewRowIndex},{e.NewColumnIndex}]");
        }

        // Separate error handlers for better debugging:
        private void OnDataServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DataService error: {e.Operation} - {e.Exception.Message}");
            OnErrorOccurred(e);
        }

        private void OnValidationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"ValidationService error: {e.Operation} - {e.Exception.Message}");
            OnErrorOccurred(e);
        }

        private void OnNavigationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"NavigationService error: {e.Operation} - {e.Exception.Message}");
            OnErrorOccurred(e);
        }

        // Events
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
    }
}