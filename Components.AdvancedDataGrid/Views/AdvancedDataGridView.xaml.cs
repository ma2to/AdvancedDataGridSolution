// RpaWpfComponents/AdvancedDataGrid/Views/AdvancedDataGridView.xaml.cs
using RpaWpfComponents.AdvancedDataGrid.Behaviors;
using RpaWpfComponents.AdvancedDataGrid.Configuration;
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RpaWpfComponents.AdvancedDataGrid.Views
{
    public partial class AdvancedDataGridView : UserControl
    {
        private AdvancedDataGridViewModel _viewModel = null!;
        private readonly ILogger<AdvancedDataGridView> _logger;

        public AdvancedDataGridView()
        {
            InitializeComponent();
            _logger = LoggerFactory.CreateLogger<AdvancedDataGridView>();
            this.Loaded += OnLoaded;
            _logger.LogDebug("AdvancedDataGridView created");
        }

        #region Public Properties and Events

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        public AdvancedDataGridViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnViewModelError;
                }

                _viewModel = value!;
                DataContext = _viewModel;

                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred += OnViewModelError;
                }
            }
        }

        #endregion

        #region Public Methods

        public async Task InitializeAsync(List<ColumnDefinitionModel> columns, List<ValidationRuleModel>? validationRules = null)
        {
            try
            {
                _logger.LogInformation("Initializing AdvancedDataGridView with {ColumnCount} columns", columns?.Count ?? 0);

                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    ViewModel = _viewModel;
                }

                await _viewModel.InitializeAsync(columns, validationRules ?? new List<ValidationRuleModel>());
                GenerateDataGridColumns(columns);
                SetupNavigationService();

                _logger.LogInformation("AdvancedDataGridView initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AdvancedDataGridView");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
            }
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                if (_viewModel == null)
                    throw new InvalidOperationException("Component must be initialized first! Call InitializeAsync() before LoadDataAsync().");

                if (!_viewModel.IsInitialized)
                    throw new InvalidOperationException("Component not properly initialized! Call InitializeAsync() with validation rules first.");

                _logger.LogInformation("Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);
                await _viewModel.LoadDataAsync(dataTable);
                _logger.LogInformation("Data loaded successfully with applied validations");
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
                if (_viewModel == null)
                    throw new InvalidOperationException("Component must be initialized first! Call InitializeAsync() before LoadDataAsync().");

                if (!_viewModel.IsInitialized)
                    throw new InvalidOperationException("Component not properly initialized! Call InitializeAsync() with validation rules first.");

                var dataTable = ConvertToDataTable(data);
                await _viewModel.LoadDataAsync(dataTable);
                _logger.LogInformation("Data loaded from dictionary list successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from dictionary list");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        public void Reset()
        {
            try
            {
                _logger.LogInformation("Resetting AdvancedDataGridView");
                _viewModel?.Reset();
                MainDataGrid.Columns.Clear();
                _logger.LogInformation("AdvancedDataGridView reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting AdvancedDataGridView");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
            }
        }

        public void Initialize(List<ColumnDefinitionModel> columns, List<ValidationRuleModel>? validationRules = null)
        {
            try
            {
                Task.Run(async () => await InitializeAsync(columns, validationRules));
                _logger.LogWarning("Used synchronous Initialize() - for full control use InitializeAsync()");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in synchronous Initialize");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Initialize"));
            }
        }

        public async Task<DataTable> ExportDataAsync()
        {
            try
            {
                if (_viewModel == null)
                    return new DataTable();

                var result = await _viewModel.ExportDataAsync();
                _logger.LogInformation("Data exported to DataTable with {RowCount} rows", result.Rows.Count);
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
                if (_viewModel == null)
                    return false;

                var result = await _viewModel.ValidateAllRowsAsync();
                _logger.LogInformation("Validation completed, all valid: {AllValid}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating all rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                if (_viewModel?.ClearAllDataCommand?.CanExecute(null) == true)
                {
                    _viewModel.ClearAllDataCommand.Execute(null);
                    _logger.LogInformation("All data cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
            }
        }

        public async Task RemoveRowsByConditionAsync(string columnName, Func<object, bool> condition)
        {
            try
            {
                if (_viewModel == null)
                    return;

                await _viewModel.RemoveRowsByConditionAsync(columnName, condition);
                _logger.LogInformation("Rows removed by condition for column: {ColumnName}", columnName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing rows by condition for column: {ColumnName}", columnName);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
            }
        }

        public async Task<int> RemoveRowsByCustomValidationAsync(List<ValidationRuleModel> customValidationRules)
        {
            try
            {
                if (_viewModel == null)
                    return 0;

                var result = await _viewModel.RemoveRowsByCustomValidationAsync(customValidationRules);
                _logger.LogInformation("Removed {RemovedCount} rows by custom validation", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing rows by custom validation");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByCustomValidationAsync"));
                return 0;
            }
        }

        public async Task RemoveEmptyRowsAsync()
        {
            try
            {
                if (_viewModel?.RemoveEmptyRowsCommand?.CanExecute(null) == true)
                {
                    _viewModel.RemoveEmptyRowsCommand.Execute(null);
                    _logger.LogInformation("Empty rows removed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        #endregion

        #region Private Methods

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel ??= CreateViewModel();
                ViewModel = _viewModel;
                SetupEventHandlers();
                _logger.LogDebug("AdvancedDataGridView loaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OnLoaded");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnLoaded"));
            }
        }

        private AdvancedDataGridViewModel CreateViewModel()
        {
            try
            {
                return DependencyInjectionConfig.GetService<AdvancedDataGridViewModel>()
                       ?? DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
            catch
            {
                return DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
        }

        private void GenerateDataGridColumns(List<ColumnDefinitionModel> columns)
        {
            try
            {
                _logger.LogDebug("Generating DataGrid columns");
                MainDataGrid.Columns.Clear();
                var orderedColumns = ReorderSpecialColumns(columns);

                foreach (var column in orderedColumns)
                {
                    DataGridColumn gridColumn;

                    if (column.Name == "DeleteAction")
                    {
                        gridColumn = CreateDeleteActionColumn();
                    }
                    else if (column.Name == "ValidAlerts")
                    {
                        gridColumn = CreateValidAlertsColumn(column);
                    }
                    else
                    {
                        gridColumn = CreateDataColumn(column);
                    }

                    MainDataGrid.Columns.Add(gridColumn);
                }

                _logger.LogInformation("Generated {ColumnCount} columns in correct order", orderedColumns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating DataGrid columns");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "GenerateDataGridColumns"));
            }
        }

        private List<ColumnDefinitionModel> ReorderSpecialColumns(List<ColumnDefinitionModel> originalColumns)
        {
            try
            {
                var result = new List<ColumnDefinitionModel>();

                var normalColumns = originalColumns.Where(c => c.Name != "ValidAlerts" && c.Name != "DeleteAction").ToList();
                result.AddRange(normalColumns);

                var deleteActionColumn = originalColumns.FirstOrDefault(c => c.Name == "DeleteAction");
                if (deleteActionColumn != null)
                {
                    deleteActionColumn.DataType = typeof(object);
                    result.Add(deleteActionColumn);
                }

                var validAlertsColumn = originalColumns.FirstOrDefault(c => c.Name == "ValidAlerts");
                if (validAlertsColumn != null)
                {
                    validAlertsColumn.DataType = typeof(string);
                    validAlertsColumn.IsReadOnly = true;
                    result.Add(validAlertsColumn);
                }
                else
                {
                    var autoValidAlertsColumn = new ColumnDefinitionModel
                    {
                        Name = "ValidAlerts",
                        DataType = typeof(string),
                        MinWidth = 150,
                        MaxWidth = 400,
                        AllowResize = true,
                        AllowSort = false,
                        IsReadOnly = true
                    };
                    result.Add(autoValidAlertsColumn);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering special columns");
                return originalColumns;
            }
        }

        private DataGridColumn CreateDataColumn(ColumnDefinitionModel column)
        {
            var gridColumn = new DataGridTextColumn
            {
                Header = column.Name,
                MinWidth = column.MinWidth,
                MaxWidth = column.MaxWidth,
                CanUserResize = column.AllowResize,
                CanUserSort = column.AllowSort,
                IsReadOnly = column.IsReadOnly,
                Binding = new Binding($"Cells[{column.Name}].Value")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            };

            var cellStyle = new Style(typeof(DataGridCell));

            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(4, 2, 4, 2)));
            cellStyle.Setters.Add(new Setter(DataGridCell.VerticalAlignmentProperty, VerticalAlignment.Stretch));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Gray));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(1, 1, 1, 1)));
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.White));

            var selectedTrigger = new Trigger
            {
                Property = DataGridCell.IsSelectedProperty,
                Value = true
            };
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(33, 150, 243))));
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(2, 2, 2, 2)));
            selectedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(173, 216, 255))));
            cellStyle.Triggers.Add(selectedTrigger);

            var validationTrigger = new DataTrigger
            {
                Binding = new Binding($"Cells[{column.Name}].HasValidationError"),
                Value = true
            };
            validationTrigger.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Red));
            validationTrigger.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(3, 3, 3, 3)));
            validationTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(255, 238, 238))));
            cellStyle.Triggers.Add(validationTrigger);

            var focusedTrigger = new Trigger
            {
                Property = DataGridCell.IsFocusedProperty,
                Value = true
            };
            focusedTrigger.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(76, 175, 80))));
            focusedTrigger.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(3, 3, 3, 3)));
            focusedTrigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(Color.FromRgb(144, 202, 249))));
            cellStyle.Triggers.Add(focusedTrigger);

            gridColumn.CellStyle = cellStyle;

            var textBlockStyle = new Style(typeof(TextBlock));
            textBlockStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            textBlockStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4, 2, 4, 2)));
            textBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));

            var textValidationTrigger = new DataTrigger
            {
                Binding = new Binding($"Cells[{column.Name}].HasValidationError"),
                Value = true
            };
            textValidationTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.DarkRed));
            textBlockStyle.Triggers.Add(textValidationTrigger);

            gridColumn.ElementStyle = textBlockStyle;

            var textBoxStyle = new Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(TextBox.TextWrappingProperty, TextWrapping.Wrap));
            textBoxStyle.Setters.Add(new Setter(TextBox.AcceptsReturnProperty, true));
            textBoxStyle.Setters.Add(new Setter(TextBox.PaddingProperty, new Thickness(4, 2, 4, 2)));
            textBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.Black));
            textBoxStyle.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(0)));
            gridColumn.EditingElementStyle = textBoxStyle;

            return gridColumn;
        }

        private DataGridColumn CreateDeleteActionColumn()
        {
            var gridColumn = new DataGridTemplateColumn
            {
                Header = "Akcie",
                Width = new DataGridLength(60, DataGridLengthUnitType.Pixel),
                CanUserResize = false,
                CanUserSort = false,
                IsReadOnly = true,
                CanUserReorder = false,
                CellTemplate = (DataTemplate)Resources["DeleteButtonTemplate"]
            };

            var cellStyle = new Style(typeof(DataGridCell));
            var neutralBackground = new SolidColorBrush(Color.FromRgb(248, 249, 250));
            var neutralBorder = Brushes.LightGray;
            var neutralThickness = new Thickness(1);

            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(2, 2, 2, 2)));
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, neutralBackground));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, neutralBorder));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, neutralThickness));

            gridColumn.CellStyle = cellStyle;
            return gridColumn;
        }

        private DataGridColumn CreateValidAlertsColumn(ColumnDefinitionModel column)
        {
            var gridColumn = new DataGridTextColumn
            {
                Header = "Validačné chyby",
                MinWidth = column.MinWidth,
                MaxWidth = column.MaxWidth,
                CanUserResize = true,
                CanUserSort = false,
                IsReadOnly = true,
                CanUserReorder = false,
                Binding = new Binding("ValidationErrorsText")
                {
                    Mode = BindingMode.OneWay
                }
            };

            var cellStyle = new Style(typeof(DataGridCell));
            var neutralBackground = new SolidColorBrush(Color.FromRgb(248, 249, 250));
            var neutralBorder = Brushes.LightGray;

            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(4, 2, 4, 2)));
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, neutralBackground));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, neutralBorder));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(1)));

            gridColumn.CellStyle = cellStyle;

            var textBlockStyle = new Style(typeof(TextBlock));
            textBlockStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            textBlockStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4, 2, 4, 2)));
            textBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.DarkRed));
            textBlockStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 10.0));
            gridColumn.ElementStyle = textBlockStyle;

            return gridColumn;
        }

        private void SetupNavigationService()
        {
            try
            {
                if (_viewModel?.NavigationService != null)
                {
                    _viewModel.NavigationService.Initialize(_viewModel.Rows.ToList(), _viewModel.Columns.Select(c => c.Model).ToList());
                    _logger.LogDebug("Navigation service setup completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up navigation service");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "SetupNavigationService"));
            }
        }

        private void SetupEventHandlers()
        {
            try
            {
                MainDataGrid.PreviewKeyDown += OnMainDataGridPreviewKeyDown;
                MainDataGrid.SelectedCellsChanged += OnSelectedCellsChanged;
                MainDataGrid.CurrentCellChanged += OnCurrentCellChanged;
                MainDataGrid.CellEditEnding += OnCellEditEnding;
                MainDataGrid.PreviewKeyDown += OnMainDataGridPreviewKeyDownForPaste;
                _logger.LogDebug("Event handlers setup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up event handlers");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "SetupEventHandlers"));
            }
        }

        private void OnMainDataGridPreviewKeyDownForPaste(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
                {
                    if (IsCurrentCellOnSpecialColumn())
                    {
                        _logger.LogDebug("PASTE blocked on special column");
                        e.Handled = true;
                        return;
                    }

                    if (_viewModel?.PasteCommand?.CanExecute(null) == true)
                    {
                        _viewModel.PasteCommand.Execute(null);
                    }

                    e.Handled = true;
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
                {
                    if (IsCurrentCellOnSpecialColumn())
                    {
                        _logger.LogDebug("COPY blocked on special column");
                        e.Handled = true;
                        return;
                    }

                    ExecuteFilteredCopy();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in clipboard operations");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnMainDataGridPreviewKeyDownForPaste"));
            }
        }

        private void ExecuteFilteredCopy()
        {
            try
            {
                var selectedCells = MainDataGrid.SelectedCells.ToList();
                if (selectedCells.Count == 0) return;

                var normalCells = selectedCells
                    .Where(cell => cell.Column?.Header is string columnName && !IsSpecialColumn(columnName))
                    .ToList();

                if (normalCells.Count == 0) return;

                var dataMap = CreateDataMapFromCells(normalCells);
                var clipboardText = ConvertDataMapToExcelFormat(dataMap);

                if (!string.IsNullOrEmpty(clipboardText))
                {
                    System.Windows.Clipboard.SetText(clipboardText);
                    _logger.LogDebug("Copied filtered data to clipboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing filtered copy");
            }
        }

        private Dictionary<(int row, int col), string> CreateDataMapFromCells(List<DataGridCellInfo> cells)
        {
            var dataMap = new Dictionary<(int row, int col), string>();
            var normalColumns = _viewModel?.Columns
                .Where(c => !c.IsSpecialColumn)
                .Select(c => c.Name)
                .ToList() ?? new List<string>();

            foreach (var cell in cells)
            {
                if (cell.Item is DataGridRowModel row &&
                    cell.Column?.Header is string columnName)
                {
                    var rowIndex = _viewModel?.Rows.IndexOf(row) ?? -1;
                    var colIndex = normalColumns.IndexOf(columnName);

                    if (rowIndex >= 0 && colIndex >= 0)
                    {
                        var cellValue = row.GetCell(columnName)?.Value?.ToString() ?? "";
                        dataMap[(rowIndex, colIndex)] = cellValue;
                    }
                }
            }

            return dataMap;
        }

        private string ConvertDataMapToExcelFormat(Dictionary<(int row, int col), string> dataMap)
        {
            if (dataMap.Count == 0) return "";

            var minRow = dataMap.Keys.Min(k => k.row);
            var maxRow = dataMap.Keys.Max(k => k.row);
            var minCol = dataMap.Keys.Min(k => k.col);
            var maxCol = dataMap.Keys.Max(k => k.col);

            var result = new List<string>();

            for (int r = minRow; r <= maxRow; r++)
            {
                var rowData = new List<string>();
                for (int c = minCol; c <= maxCol; c++)
                {
                    var cellValue = dataMap.TryGetValue((r, c), out var value) ? value : "";
                    rowData.Add(cellValue);
                }
                result.Add(string.Join("\t", rowData));
            }

            return string.Join("\n", result);
        }

        private void OnMainDataGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Additional key handling if needed
        }

        private void OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                UpdateCurrentCellFromSelection();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling selected cells changed");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnSelectedCellsChanged"));
            }
        }

        private void OnCurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                UpdateCurrentCellFromSelection();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling current cell changed");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnCurrentCellChanged"));
            }
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.Row.Item is DataGridRowModel row)
                {
                    _ = Task.Run(async () => await _viewModel?.ValidateRowAsync(row));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell edit ending");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnCellEditEnding"));
            }
        }

        private void UpdateCurrentCellFromSelection()
        {
            try
            {
                if (MainDataGrid.CurrentCell.IsValid &&
                    MainDataGrid.CurrentCell.Item is DataGridRowModel row &&
                    MainDataGrid.CurrentCell.Column?.Header is string columnName &&
                    _viewModel?.NavigationService != null)
                {
                    var rowIndex = _viewModel.Rows.IndexOf(row);
                    var editableColumns = _viewModel.Columns.Where(c => !c.IsSpecialColumn).ToList();
                    var columnIndex = editableColumns.FindIndex(c => c.Name == columnName);

                    if (rowIndex >= 0 && columnIndex >= 0)
                    {
                        _viewModel.NavigationService.MoveToCell(rowIndex, columnIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating current cell from selection");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "UpdateCurrentCellFromSelection"));
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

        private bool IsCurrentCellOnSpecialColumn()
        {
            try
            {
                if (MainDataGrid.CurrentCell.IsValid &&
                    MainDataGrid.CurrentCell.Column?.Header is string columnName)
                {
                    return IsSpecialColumn(columnName);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if current cell is on special column");
                return false;
            }
        }

        private bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        private void OnViewModelError(object sender, ComponentErrorEventArgs e)
        {
            OnErrorOccurred(e);
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        #endregion
    }
}