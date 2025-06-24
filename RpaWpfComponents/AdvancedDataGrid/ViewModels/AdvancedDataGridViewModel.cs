// ===========================================
// RpaWpfComponents/AdvancedDataGrid/ViewModels/AdvancedDataGridViewModel.cs - KOMPLETNÁ NÁHRADA
// ===========================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using RpaWpfComponents.AdvancedDataGrid.Commands;
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Collections;

namespace RpaWpfComponents.AdvancedDataGrid.ViewModels
{
    public class AdvancedDataGridViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IDataService _dataService;
        private readonly IValidationService _validationService;
        private readonly IClipboardService _clipboardService;
        private readonly IColumnService _columnService;
        private readonly IExportService _exportService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<AdvancedDataGridViewModel> _logger;

        private ObservableRangeCollection<DataGridRowModel> _rows = new ObservableRangeCollection<DataGridRowModel>();
        private ObservableRangeCollection<ColumnDefinitionViewModel> _columns = new ObservableRangeCollection<ColumnDefinitionViewModel>();
        private bool _isValidating = false;
        private double _validationProgress = 0;
        private string _validationStatus = "Pripravené";
        private bool _isInitialized = false;
        private bool _disposed = false;

        // NOVÉ: Separátny počet riadkov
        private int _initialRowCount = 100;

        // ✅ NOVÉ: Keyboard Shortcuts Toggle
        private bool _isKeyboardShortcutsVisible = false; // Default: skryté

        // Throttling support
        private ThrottlingConfiguration _throttlingConfig = ThrottlingConfiguration.Default;
        private readonly Dictionary<string, System.Threading.CancellationTokenSource> _pendingValidations = new();
        private System.Threading.SemaphoreSlim _validationSemaphore;

        public AdvancedDataGridViewModel(
            IDataService dataService,
            IValidationService validationService,
            IClipboardService clipboardService,
            IColumnService columnService,
            IExportService exportService,
            INavigationService navigationService,
            ILogger<AdvancedDataGridViewModel> logger = null)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _columnService = columnService ?? throw new ArgumentNullException(nameof(columnService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdvancedDataGridViewModel>.Instance;

            // Initialize validation semaphore with default max concurrent validations
            _validationSemaphore = new System.Threading.SemaphoreSlim(_throttlingConfig.MaxConcurrentValidations, _throttlingConfig.MaxConcurrentValidations);

            InitializeCollections();
            InitializeCommands();
            SubscribeToEvents();

            _logger.LogDebug("AdvancedDataGridViewModel created with default settings");
        }

        #region IDisposable Implementation

        /// <summary>
        /// Dispose pattern implementation for proper memory cleanup
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _logger?.LogDebug("Disposing AdvancedDataGridViewModel...");

                    // Unsubscribe from all events
                    UnsubscribeFromEvents();

                    // Dispose services if they implement IDisposable
                    DisposeServices();

                    // Clear collections
                    ClearCollections();

                    // Clear commands
                    ClearCommands();

                    _isInitialized = false;

                    _logger?.LogInformation("AdvancedDataGridViewModel disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during AdvancedDataGridViewModel disposal");
                }
            }

            _disposed = true;
        }

        private void UnsubscribeFromEvents()
        {
            try
            {
                if (_dataService != null)
                {
                    _dataService.DataChanged -= OnDataChanged;
                    _dataService.ErrorOccurred -= OnDataServiceErrorOccurred;
                }

                if (_validationService != null)
                {
                    _validationService.ValidationCompleted -= OnValidationCompleted;
                    _validationService.ValidationErrorOccurred -= OnValidationServiceErrorOccurred;
                }

                if (_navigationService != null)
                {
                    _navigationService.ErrorOccurred -= OnNavigationServiceErrorOccurred;
                }

                _logger?.LogDebug("All service events unsubscribed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unsubscribing from service events");
            }
        }

        private void DisposeServices()
        {
            try
            {
                // Dispose services if they implement IDisposable
                if (_dataService is IDisposable disposableDataService)
                    disposableDataService.Dispose();

                if (_validationService is IDisposable disposableValidationService)
                    disposableValidationService.Dispose();

                if (_clipboardService is IDisposable disposableClipboardService)
                    disposableClipboardService.Dispose();

                if (_columnService is IDisposable disposableColumnService)
                    disposableColumnService.Dispose();

                if (_exportService is IDisposable disposableExportService)
                    disposableExportService.Dispose();

                if (_navigationService is IDisposable disposableNavigationService)
                    disposableNavigationService.Dispose();

                _logger?.LogDebug("Services disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing services");
            }
        }

        private void ClearCollections()
        {
            try
            {
                // Cancel all pending validations
                foreach (var cts in _pendingValidations.Values)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                _pendingValidations.Clear();

                // Clear rows and unsubscribe from cell events
                if (_rows?.Count > 0)
                {
                    foreach (var row in _rows)
                    {
                        foreach (var cell in row.Cells.Values)
                        {
                            // Unsubscribe from cell value changed events
                            // Note: We can't easily unsubscribe anonymous lambdas, 
                            // but they will be GC'd when the cell is disposed
                        }
                    }
                }

                _rows?.Clear();
                _columns?.Clear();

                // Dispose semaphore
                _validationSemaphore?.Dispose();

                _logger?.LogDebug("Collections and throttling resources cleared successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing collections");
            }
        }

        private void ClearCommands()
        {
            try
            {
                ValidateAllCommand = null;
                ClearAllDataCommand = null;
                RemoveEmptyRowsCommand = null;
                CopyCommand = null;
                PasteCommand = null;
                DeleteRowCommand = null;
                ExportToDataTableCommand = null;
                ToggleKeyboardShortcutsCommand = null; // ✅ PRIDANÉ

                _logger?.LogDebug("Commands cleared successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing commands");
            }
        }

        /// <summary>
        /// Finalizer - only if we have unmanaged resources
        /// </summary>
        ~AdvancedDataGridViewModel()
        {
            Dispose(false);
        }

        /// <summary>
        /// Check if object is disposed
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdvancedDataGridViewModel));
        }

        #endregion

        #region Properties

        public ObservableRangeCollection<DataGridRowModel> Rows
        {
            get
            {
                ThrowIfDisposed();
                return _rows;
            }
            set => SetProperty(ref _rows, value);
        }

        public ObservableRangeCollection<ColumnDefinitionViewModel> Columns
        {
            get
            {
                ThrowIfDisposed();
                return _columns;
            }
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

        public INavigationService NavigationService
        {
            get
            {
                ThrowIfDisposed();
                return _navigationService;
            }
        }

        public bool IsInitialized
        {
            get
            {
                if (_disposed) return false;
                return _isInitialized;
            }
        }

        /// <summary>
        /// NOVÉ: Počet riadkov nastavený pri inicializácii
        /// </summary>
        public int InitialRowCount
        {
            get
            {
                ThrowIfDisposed();
                return _initialRowCount;
            }
        }

        /// <summary>
        /// Konfigurácia throttling pre real-time validáciu
        /// </summary>
        public ThrottlingConfiguration ThrottlingConfig
        {
            get
            {
                ThrowIfDisposed();
                return _throttlingConfig;
            }
        }

        /// <summary>
        /// ✅ NOVÉ: Indikuje či sú klávesové skratky zobrazené
        /// </summary>
        public bool IsKeyboardShortcutsVisible
        {
            get => _isKeyboardShortcutsVisible;
            set => SetProperty(ref _isKeyboardShortcutsVisible, value);
        }

        #endregion

        #region Commands

        public ICommand ValidateAllCommand { get; private set; }
        public ICommand ClearAllDataCommand { get; private set; }
        public ICommand RemoveEmptyRowsCommand { get; private set; }
        public ICommand CopyCommand { get; private set; }
        public ICommand PasteCommand { get; private set; }
        public ICommand DeleteRowCommand { get; private set; }
        public ICommand ExportToDataTableCommand { get; private set; }

        /// <summary>
        /// ✅ NOVÉ: Command pre zobrazenie/skrytie klávesových skratiek
        /// </summary>
        public ICommand ToggleKeyboardShortcutsCommand { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// NOVÉ API: Inicializácia s možnosťou nastavenia počtu riadkov
        /// </summary>
        public async Task InitializeAsync(
            List<ColumnDefinitionModel> columnDefinitions,
            List<ValidationRuleModel> validationRules = null,
            ThrottlingConfiguration throttling = null,
            int initialRowCount = 100)
        {
            ThrowIfDisposed();

            try
            {
                if (_isInitialized)
                {
                    _logger.LogWarning("Component already initialized. Call Reset() first if needed.");
                    return;
                }

                // Nastav počet riadkov s validáciou
                _initialRowCount = Math.Max(1, Math.Min(initialRowCount, 10000)); // Safety limits

                // Configure throttling
                _throttlingConfig = throttling ?? ThrottlingConfiguration.Default;
                _throttlingConfig.Validate();

                // Update semaphore with new max concurrent validations
                _validationSemaphore?.Dispose();
                _validationSemaphore = new System.Threading.SemaphoreSlim(_throttlingConfig.MaxConcurrentValidations, _throttlingConfig.MaxConcurrentValidations);

                _logger.LogInformation("Initializing AdvancedDataGrid with {ColumnCount} columns, {RuleCount} validation rules, {InitialRowCount} rows, throttling: {TypingDelay}ms",
                    columnDefinitions?.Count ?? 0, validationRules?.Count ?? 0, _initialRowCount, _throttlingConfig.TypingDelayMs);

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
                _logger.LogInformation("AdvancedDataGrid initialization completed: {ActualRowCount} rows created, throttling: {IsEnabled}",
                    Rows.Count, _throttlingConfig.IsEnabled);
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
            ThrowIfDisposed();

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

                // AUTO-EXPANSION LOGIKA
                // Ak dataset má menej riadkov ako InitialRowCount, zachová sa InitialRowCount
                // Ak dataset má viac riadkov, grid sa rozšíri na dataset + nejaké prázdne riadky
                var minEmptyRows = Math.Min(10, _initialRowCount / 5); // Aspoň 10 prázdnych riadkov alebo 20% z InitialRowCount
                var finalRowCount = Math.Max(_initialRowCount, totalRows + minEmptyRows);

                while (newRows.Count < finalRowCount)
                {
                    newRows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                Rows.AddRange(newRows);

                ValidationStatus = "Validácia dokončená";
                ValidationProgress = 100;

                var validRows = newRows.Count(r => !r.IsEmpty && !r.HasValidationErrors);
                var invalidRows = newRows.Count(r => !r.IsEmpty && r.HasValidationErrors);
                var emptyRows = newRows.Count - totalRows;

                _logger.LogInformation("Data loaded with auto-expansion: {TotalRows} total rows ({DataRows} data, {EmptyRows} empty), {ValidRows} valid, {InvalidRows} invalid, initial config was {InitialRowCount}",
                    newRows.Count, totalRows, emptyRows, validRows, invalidRows, _initialRowCount);

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
            ThrowIfDisposed();

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

        public async Task<DataTable> ExportDataAsync(bool includeValidAlerts = false)
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Exporting data to DataTable, includeValidAlerts: {IncludeValidAlerts}", includeValidAlerts);
                var columnDefinitions = Columns.Select(c => c.Model).ToList();
                var result = await _exportService.ExportToDataTableAsync(Rows.ToList(), columnDefinitions, includeValidAlerts);
                _logger.LogInformation("Exported {RowCount} rows to DataTable, includeValidAlerts: {IncludeValidAlerts}", result.Rows.Count, includeValidAlerts);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
                return new DataTable();
            }
        }

        public async Task<string> ExportToCsvAsync(bool includeValidAlerts = false)
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Exporting data to CSV, includeValidAlerts: {IncludeValidAlerts}", includeValidAlerts);
                var columnDefinitions = Columns.Select(c => c.Model).ToList();
                var result = await _exportService.ExportToCsvAsync(Rows.ToList(), columnDefinitions, includeValidAlerts);
                _logger.LogInformation("Exported data to CSV, length: {Length}, includeValidAlerts: {IncludeValidAlerts}", result.Length, includeValidAlerts);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to CSV");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToCsvAsync"));
                return string.Empty;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(bool includeValidAlerts = false)
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Exporting data to Excel, includeValidAlerts: {IncludeValidAlerts}", includeValidAlerts);
                var columnDefinitions = Columns.Select(c => c.Model).ToList();
                var result = await _exportService.ExportToExcelAsync(Rows.ToList(), columnDefinitions, includeValidAlerts);
                _logger.LogInformation("Exported data to Excel, bytes: {ByteCount}, includeValidAlerts: {IncludeValidAlerts}", result.Length, includeValidAlerts);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to Excel");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToExcelAsync"));
                return new byte[0];
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            ThrowIfDisposed();

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
            ThrowIfDisposed();

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

        public async Task<int> RemoveRowsByCustomValidationAsync(List<ValidationRuleModel> customValidationRules)
        {
            ThrowIfDisposed();

            try
            {
                if (!_isInitialized || customValidationRules?.Count == 0)
                    return 0;

                _logger.LogDebug("Removing rows by custom validation with {RuleCount} rules", customValidationRules.Count);

                var result = await Task.Run(() =>
                {
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

                    return rowsToRemove;
                });

                foreach (var row in result)
                {
                    Rows.Remove(row);
                }

                // Ensure we have enough empty rows based on InitialRowCount
                var currentEmptyRows = Rows.Count(r => r.IsEmpty);
                var minEmptyRows = Math.Min(10, _initialRowCount / 5);
                var neededEmptyRows = Math.Max(minEmptyRows, _initialRowCount - Rows.Count(r => !r.IsEmpty));

                for (int i = currentEmptyRows; i < neededEmptyRows; i++)
                {
                    Rows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                _logger.LogInformation("Removed {RowCount} rows by custom validation", result.Count);
                return result.Count;
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
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Removing rows by condition for column: {ColumnName}", columnName);

                var result = await Task.Run(() =>
                {
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

                    return rowsToRemove;
                });

                foreach (var row in result)
                {
                    Rows.Remove(row);
                }

                // Ensure we have enough empty rows based on InitialRowCount
                var currentEmptyRows = Rows.Count(r => r.IsEmpty);
                var minEmptyRows = Math.Min(10, _initialRowCount / 5);
                var neededEmptyRows = Math.Max(minEmptyRows, _initialRowCount - Rows.Count(r => !r.IsEmpty));

                for (int i = currentEmptyRows; i < neededEmptyRows; i++)
                {
                    Rows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                _logger.LogInformation("Removed {RowCount} rows by condition for column: {ColumnName}", result.Count, columnName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing rows by condition for column: {ColumnName}", columnName);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
            }
        }

        #endregion

        #region Public Methods - Advanced

        /// <summary>
        /// Reset ViewModelu do pôvodného stavu.
        /// POZOR: Táto funkcia vymaže všetko vrátane stĺpcov a validácií!
        /// Pre bežné použitie radšej používajte ClearAllDataAsync().
        /// </summary>
        public void Reset()
        {
            if (_disposed) return; // Ak je disposed, nerobíme reset

            try
            {
                _logger.LogInformation("Resetting ViewModel (for cleanup)");

                // Clear collections with proper cleanup
                ClearCollections();

                _validationService.ClearValidationRules();
                _isInitialized = false;

                IsValidating = false;
                ValidationProgress = 0;
                ValidationStatus = "Pripravené";

                // Reset row count to default
                _initialRowCount = 100;

                // Reset keyboard shortcuts visibility
                IsKeyboardShortcutsVisible = false;

                _logger.LogInformation("ViewModel reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ViewModel reset");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
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

            // ✅ NOVÉ: Command pre toggle klávesových skratiek
            ToggleKeyboardShortcutsCommand = new RelayCommand(ToggleKeyboardShortcuts);
        }

        /// <summary>
        /// ✅ NOVÁ METÓDA: Toggle klávesových skratiek
        /// </summary>
        private void ToggleKeyboardShortcuts()
        {
            if (_disposed) return;

            try
            {
                IsKeyboardShortcutsVisible = !IsKeyboardShortcutsVisible;
                _logger.LogDebug("Keyboard shortcuts visibility toggled to: {IsVisible}", IsKeyboardShortcutsVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling keyboard shortcuts visibility");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ToggleKeyboardShortcuts"));
            }
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
            // POUŽÍVA _initialRowCount namiesto throttling konfigurácie
            var rowCount = _initialRowCount;

            var rows = await Task.Run(() =>
            {
                var rowList = new List<DataGridRowModel>();

                for (int i = 0; i < rowCount; i++)
                {
                    var row = CreateEmptyRowWithRealTimeValidation();
                    rowList.Add(row);
                }

                return rowList;
            });

            Rows.Clear();
            Rows.AddRange(rows);

            _logger.LogDebug("Created {RowCount} initial empty rows based on InitialRowCount setting", rowCount);
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
            if (_disposed) return;

            try
            {
                // If throttling is disabled, validate immediately
                if (!_throttlingConfig.IsEnabled)
                {
                    await ValidateCellImmediately(row, cell);
                    return;
                }

                // Create unique key for this cell
                var cellKey = $"{Rows.IndexOf(row)}_{cell.ColumnName}";

                // Cancel previous validation for this cell
                if (_pendingValidations.TryGetValue(cellKey, out var existingCts))
                {
                    existingCts.Cancel();
                    _pendingValidations.Remove(cellKey);
                }

                // If row is empty, clear validation immediately
                if (row.IsEmpty)
                {
                    cell.SetValidationErrors(new List<string>());
                    row.UpdateValidationStatus();
                    return;
                }

                // Create new cancellation token for this validation
                var cts = new System.Threading.CancellationTokenSource();
                _pendingValidations[cellKey] = cts;

                try
                {
                    // Apply throttling delay
                    await Task.Delay(_throttlingConfig.TypingDelayMs, cts.Token);

                    // Check if still valid (not cancelled and not disposed)
                    if (cts.Token.IsCancellationRequested || _disposed)
                        return;

                    // Perform throttled validation
                    await ValidateCellThrottled(row, cell, cellKey, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Validation was cancelled - this is normal
                    _logger.LogTrace("Validation cancelled for cell: {CellKey}", cellKey);
                }
                finally
                {
                    // Clean up
                    _pendingValidations.Remove(cellKey);
                    cts.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in throttled cell validation");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnCellValueChangedRealTime"));
            }
        }

        private async Task ValidateCellImmediately(DataGridRowModel row, DataGridCellModel cell)
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
                _logger.LogError(ex, "Error in immediate cell validation");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellImmediately"));
            }
        }

        private async Task ValidateCellThrottled(DataGridRowModel row, DataGridCellModel cell, string cellKey, System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                // Use semaphore to limit concurrent validations
                await _validationSemaphore.WaitAsync(cancellationToken);

                try
                {
                    // Double-check if still valid
                    if (cancellationToken.IsCancellationRequested || _disposed)
                        return;

                    _logger.LogTrace("Executing throttled validation for cell: {CellKey}", cellKey);

                    // Perform actual validation
                    await _validationService.ValidateCellAsync(cell, row);
                    row.UpdateValidationStatus();

                    _logger.LogTrace("Throttled validation completed for cell: {CellKey}", cellKey);
                }
                finally
                {
                    _validationSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when validation is cancelled
                _logger.LogTrace("Throttled validation cancelled for cell: {CellKey}", cellKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in throttled validation for cell: {CellKey}", cellKey);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellThrottled"));
            }
        }

        private async Task ClearAllDataAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (!_isInitialized) return;

                _logger.LogDebug("Clearing all data");

                await Task.Run(() =>
                {
                    foreach (var row in Rows)
                    {
                        foreach (var cell in row.Cells.Values.Where(c => !_columnService.IsSpecialColumn(c.ColumnName)))
                        {
                            cell.Value = null;
                            cell.SetValidationErrors(new List<string>());
                        }
                    }
                });

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
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Removing empty rows");

                var result = await Task.Run(() =>
                {
                    var dataRows = Rows.Where(r => !r.IsEmpty).ToList();

                    // POUŽÍVA _initialRowCount namiesto throttling konfigurácie
                    var minEmptyRows = Math.Min(10, _initialRowCount / 5);
                    var emptyRowsNeeded = Math.Max(minEmptyRows, _initialRowCount - dataRows.Count);

                    var newEmptyRows = new List<DataGridRowModel>();
                    for (int i = 0; i < emptyRowsNeeded; i++)
                    {
                        newEmptyRows.Add(CreateEmptyRowWithRealTimeValidation());
                    }

                    return new { DataRows = dataRows, EmptyRows = newEmptyRows };
                });

                Rows.Clear();
                Rows.AddRange(result.DataRows);
                Rows.AddRange(result.EmptyRows);

                _logger.LogInformation("Empty rows removed, {DataRowCount} data rows kept, {EmptyRowCount} empty rows added based on InitialRowCount ({InitialRowCount})",
                    result.DataRows.Count, result.EmptyRows.Count, _initialRowCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        private async Task CopySelectedCellsAsync()
        {
            ThrowIfDisposed();

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
            ThrowIfDisposed();

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

                        // Apply paste throttling delay before triggering validations
                        if (_throttlingConfig.IsEnabled && _throttlingConfig.PasteDelayMs > 0)
                        {
                            await Task.Delay(_throttlingConfig.PasteDelayMs);
                        }

                        _logger.LogDebug("Pasted data from clipboard at position [{Row},{Col}] with {PasteDelay}ms throttling",
                            startRowIndex, startColumnIndex, _throttlingConfig.PasteDelayMs);
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
                var prepResult = await Task.Run(() =>
                {
                    int dataRows = data.GetLength(0);
                    int dataCols = data.GetLength(1);
                    var editableColumns = Columns.Where(c => !c.IsSpecialColumn).ToList();

                    var newRowsNeeded = Math.Max(0, (startRowIndex + dataRows) - Rows.Count);
                    var additionalRows = new List<DataGridRowModel>();

                    for (int i = 0; i < newRowsNeeded; i++)
                    {
                        additionalRows.Add(CreateEmptyRowWithRealTimeValidation());
                    }

                    return new
                    {
                        DataRows = dataRows,
                        DataCols = dataCols,
                        EditableColumns = editableColumns,
                        AdditionalRows = additionalRows
                    };
                });

                // Add any needed rows
                foreach (var row in prepResult.AdditionalRows)
                {
                    Rows.Add(row);
                }

                // Set the data
                for (int i = 0; i < prepResult.DataRows; i++)
                {
                    int targetRowIndex = startRowIndex + i;
                    if (targetRowIndex >= Rows.Count) break;

                    for (int j = 0; j < prepResult.DataCols; j++)
                    {
                        int targetColumnIndex = startColumnIndex + j;
                        if (targetColumnIndex >= prepResult.EditableColumns.Count) break;

                        var columnName = prepResult.EditableColumns[targetColumnIndex].Name;
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

        private void DeleteRow(DataGridRowModel row)
        {
            if (_disposed) return;

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

        private void OnDataChanged(object sender, DataChangeEventArgs e)
        {
            if (_disposed) return;
            _logger.LogTrace("Data changed: {ChangeType}", e.ChangeType);
        }

        private void OnValidationCompleted(object sender, ValidationCompletedEventArgs e)
        {
            if (_disposed) return;
            _logger.LogTrace("Validation completed for row. Is valid: {IsValid}", e.IsValid);
        }

        private void OnDataServiceErrorOccurred(object sender, ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "DataService error: {Operation}", e.Operation);
            OnErrorOccurred(e);
        }

        private void OnValidationServiceErrorOccurred(object sender, ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "ValidationService error: {Operation}", e.Operation);
            OnErrorOccurred(e);
        }

        private void OnNavigationServiceErrorOccurred(object sender, ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "NavigationService error: {Operation}", e.Operation);
            OnErrorOccurred(e);
        }

        #endregion

        #region Events & Property Changed

        public event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (_disposed) return false;

            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (_disposed) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}