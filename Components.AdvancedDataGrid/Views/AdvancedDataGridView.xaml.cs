// Views/AdvancedDataGridView.xaml.cs - OPRAVENÝ
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
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.ViewModels;
using Components.AdvancedDataGrid.Configuration;
using Components.AdvancedDataGrid.Events;

namespace Components.AdvancedDataGrid.Views
{
    public partial class AdvancedDataGridView : UserControl
    {
        private AdvancedDataGridViewModel _viewModel = null!;

        public AdvancedDataGridView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        #region Public Properties and Events

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// Získanie alebo nastavenie ViewModel
        /// </summary>
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

        /// <summary>
        /// Vlastnosť pre zobrazenie/skrytie Mirror Editora
        /// </summary>
        public bool ShowMirrorEditor
        {
            get => _viewModel?.ShowMirrorEditor ?? true;
            set
            {
                if (_viewModel != null)
                {
                    _viewModel.ShowMirrorEditor = value;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inicializácia komponentu s definíciami stĺpcov a validačnými pravidlami
        /// </summary>
        public async Task InitializeAsync(List<ColumnDefinitionModel> columns, List<ValidationRuleModel>? validationRules = null)
        {
            try
            {
                // Vytvor ViewModel ak neexistuje
                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    ViewModel = _viewModel;
                }

                // Inicializuj ViewModel
                await _viewModel.InitializeAsync(columns, validationRules ?? new List<ValidationRuleModel>());

                // Vygeneruj stĺpce v DataGrid
                GenerateDataGridColumns(columns);

                // Nastav navigation service po generovaní stĺpcov
                SetupNavigationService();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
            }
        }

        /// <summary>
        /// Synchronná verzia inicializácie pre jednoduchšie použitie
        /// </summary>
        public void Initialize(List<ColumnDefinitionModel> columns, List<ValidationRuleModel>? validationRules = null)
        {
            Task.Run(async () => await InitializeAsync(columns, validationRules));
        }

        /// <summary>
        /// Načítanie dát z DataTable
        /// </summary>
        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                if (_viewModel == null)
                    throw new InvalidOperationException("Component must be initialized first.");

                await _viewModel.LoadDataAsync(dataTable);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        /// <summary>
        /// Načítanie dát zo slovníkov
        /// </summary>
        public async Task LoadDataAsync(List<Dictionary<string, object>> data)
        {
            try
            {
                if (_viewModel == null)
                    throw new InvalidOperationException("Component must be initialized first.");

                // Konvertuj na DataTable
                var dataTable = ConvertToDataTable(data);
                await _viewModel.LoadDataAsync(dataTable);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
            }
        }

        /// <summary>
        /// Export dát do DataTable
        /// </summary>
        public async Task<DataTable> ExportDataAsync()
        {
            try
            {
                if (_viewModel == null)
                    return new DataTable();

                return await _viewModel.ExportDataAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
                return new DataTable();
            }
        }

        /// <summary>
        /// Validácia všetkých riadkov
        /// </summary>
        public async Task<bool> ValidateAllRowsAsync()
        {
            try
            {
                if (_viewModel == null)
                    return false;

                return await _viewModel.ValidateAllRowsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        /// <summary>
        /// Vymazanie všetkých dát
        /// </summary>
        public async Task ClearAllDataAsync()
        {
            try
            {
                if (_viewModel?.ClearAllDataCommand?.CanExecute(null) == true)
                {
                    _viewModel.ClearAllDataCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
            }
        }

        /// <summary>
        /// Odstránenie riadkov podľa podmienky
        /// </summary>
        public async Task RemoveRowsByConditionAsync(string columnName, Func<object, bool> condition)
        {
            try
            {
                if (_viewModel == null)
                    return;

                await _viewModel.RemoveRowsByConditionAsync(columnName, condition);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
            }
        }

        /// <summary>
        /// Odstránenie prázdnych riadkov
        /// </summary>
        public async Task RemoveEmptyRowsAsync()
        {
            try
            {
                if (_viewModel?.RemoveEmptyRowsCommand?.CanExecute(null) == true)
                {
                    _viewModel.RemoveEmptyRowsCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        #endregion

        #region Private Methods

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ak ViewModel nebol vytvorený, vytvor default
                _viewModel ??= CreateViewModel();
                ViewModel = _viewModel;

                // Nastavenie focus handlingu pre Mirror Editor
                SetupMirrorEditorHandling();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnLoaded"));
            }
        }

        private AdvancedDataGridViewModel CreateViewModel()
        {
            try
            {
                // Pokús sa použiť DI
                return DependencyInjectionConfig.GetService<AdvancedDataGridViewModel>()
                       ?? DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
            catch
            {
                // Fallback na manuálne vytvorenie
                return DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
        }

        private void GenerateDataGridColumns(List<ColumnDefinitionModel> columns)
        {
            try
            {
                MainDataGrid.Columns.Clear();

                foreach (var column in columns)
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
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "GenerateDataGridColumns"));
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

            // Style pre text
            var textBlockStyle = new Style(typeof(TextBlock));
            textBlockStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            textBlockStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4, 2, 4, 2)));

            // Trigger pre validation error styling na text
            var validationTrigger = new DataTrigger
            {
                Binding = new Binding($"Cells[{column.Name}].HasValidationError"),
                Value = true
            };
            validationTrigger.Setters.Add(new Setter(TextBlock.BackgroundProperty, new SolidColorBrush(Color.FromRgb(255, 238, 238))));
            validationTrigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.DarkRed));
            textBlockStyle.Triggers.Add(validationTrigger);

            gridColumn.ElementStyle = textBlockStyle;

            // Style pre editovanie 
            var textBoxStyle = new Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(TextBox.TextWrappingProperty, TextWrapping.Wrap));
            textBoxStyle.Setters.Add(new Setter(TextBox.AcceptsReturnProperty, true));
            textBoxStyle.Setters.Add(new Setter(TextBox.PaddingProperty, new Thickness(4, 2, 4, 2)));
            gridColumn.EditingElementStyle = textBoxStyle;

            return gridColumn;
        }

        private DataGridColumn CreateDeleteActionColumn()
        {
            return new DataGridTemplateColumn
            {
                Header = "Akcie",
                Width = new DataGridLength(60, DataGridLengthUnitType.Pixel),
                CanUserResize = false,
                CanUserSort = false,
                IsReadOnly = true,
                CellTemplate = (DataTemplate)Resources["DeleteButtonTemplate"]
            };
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
                Binding = new Binding("ValidationErrorsText")
                {
                    Mode = BindingMode.OneWay
                }
            };

            // Style pre text wrapping
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
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "SetupNavigationService"));
            }
        }

        private void SetupMirrorEditorHandling()
        {
            try
            {
                // Nastavenie handling pre keyboard navigation medzi DataGrid a Mirror Editor
                MainDataGrid.PreviewKeyDown += OnMainDataGridKeyDown;
                MainDataGrid.SelectedCellsChanged += OnSelectedCellsChanged;
                MainDataGrid.CurrentCellChanged += OnCurrentCellChanged;
                MainDataGrid.CellEditEnding += OnCellEditEnding;

                // Handling pre Ctrl+C a Ctrl+V
                MainDataGrid.KeyDown += OnMainDataGridKeyDown_Shortcuts;

                if (MirrorTextBox != null)
                {
                    MirrorTextBox.PreviewKeyDown += OnMirrorTextBoxKeyDown;
                    MirrorTextBox.GotFocus += OnMirrorTextBoxGotFocus;
                    MirrorTextBox.LostFocus += OnMirrorTextBoxLostFocus;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "SetupMirrorEditorHandling"));
            }
        }

        private void OnMainDataGridKeyDown_Shortcuts(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    switch (e.Key)
                    {
                        case Key.C:
                            if (_viewModel?.CopyCommand?.CanExecute(null) == true)
                            {
                                _viewModel.CopyCommand.Execute(null);
                                e.Handled = true;
                            }
                            break;
                        case Key.V:
                            if (_viewModel?.PasteCommand?.CanExecute(null) == true)
                            {
                                _viewModel.PasteCommand.Execute(null);
                                e.Handled = true;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnMainDataGridKeyDown_Shortcuts"));
            }
        }

        private void OnMainDataGridKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Handling pre špeciálne klávesy
                if (e.Key == Key.F2 && ShowMirrorEditor)
                {
                    // F2 = focus na Mirror Editor
                    MirrorTextBox?.Focus();
                    MirrorTextBox?.SelectAll();
                    if (_viewModel?.MirrorEditor != null)
                    {
                        _viewModel.MirrorEditor.StartEditing();
                    }
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    // ESC = cancel current edit
                    MainDataGrid.CancelEdit();
                    if (_viewModel?.MirrorEditor != null)
                    {
                        _viewModel.MirrorEditor.CancelChanges();
                    }
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnMainDataGridKeyDown"));
            }
        }

        private void OnMirrorTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Escape)
                {
                    // ESC = cancel a návrat na DataGrid
                    _viewModel?.MirrorEditor?.CancelChanges();
                    MainDataGrid.Focus();
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    // Enter (bez Shift) = commit a návrat na DataGrid
                    _viewModel?.MirrorEditor?.CommitChanges();
                    MainDataGrid.Focus();
                    e.Handled = true;
                }
                else if (e.Key == Key.Tab)
                {
                    // Tab = commit a move to next cell
                    _viewModel?.MirrorEditor?.CommitChanges();
                    _viewModel?.NavigationService?.MoveToNextCell();
                    MainDataGrid.Focus();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnMirrorTextBoxKeyDown"));
            }
        }

        private void OnMirrorTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel?.MirrorEditor != null)
                {
                    _viewModel.MirrorEditor.StartEditing();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnMirrorTextBoxGotFocus"));
            }
        }

        private void OnMirrorTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                // Auto-commit when losing focus (if not cancelled)
                if (_viewModel?.MirrorEditor != null && _viewModel.MirrorEditor.IsEditing)
                {
                    _viewModel.MirrorEditor.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnMirrorTextBoxLostFocus"));
            }
        }

        private void OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                UpdateCurrentCellFromSelection();
            }
            catch (Exception ex)
            {
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
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnCurrentCellChanged"));
            }
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                // Trigger validation when cell edit ends
                if (e.Row.Item is DataGridRowModel row)
                {
                    Task.Run(async () => await _viewModel?.ValidateRowAsync(row));
                }
            }
            catch (Exception ex)
            {
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

                        // Manuálne update Mirror Editor ak sa event nespustí
                        var cell = row.GetCell(columnName);
                        if (cell != null && _viewModel.MirrorEditor != null)
                        {
                            _viewModel.MirrorEditor.SetCurrentCell(cell);
                            System.Diagnostics.Debug.WriteLine($"Mirror Editor updated with cell: {columnName} = '{cell.Value}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "UpdateCurrentCellFromSelection"));
            }
        }

        private DataTable ConvertToDataTable(List<Dictionary<string, object>> data)
        {
            var dataTable = new DataTable();

            if (data?.Count > 0)
            {
                // Vytvor stĺpce na základe prvého riadku
                foreach (var key in data[0].Keys)
                {
                    dataTable.Columns.Add(key, typeof(object));
                }

                // Pridaj dáta
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