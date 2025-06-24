// RpaWpfComponents/AdvancedDataGrid/Behaviors/KeyboardNavigationBehavior.cs
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RpaWpfComponents.AdvancedDataGrid.Configuration;

namespace RpaWpfComponents.AdvancedDataGrid.Behaviors
{
    internal class KeyboardNavigationBehavior : Behavior<DataGrid>
    {
        private readonly ILogger<KeyboardNavigationBehavior> _logger;

        public KeyboardNavigationBehavior()
        {
            _logger = LoggerFactory.CreateLogger<KeyboardNavigationBehavior>();
        }

        public static readonly DependencyProperty NavigationServiceProperty =
            DependencyProperty.Register(nameof(NavigationService), typeof(INavigationService), typeof(KeyboardNavigationBehavior));

        public INavigationService NavigationService
        {
            get => (INavigationService)GetValue(NavigationServiceProperty);
            set => SetValue(NavigationServiceProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            AssociatedObject.BeginningEdit += OnBeginningEdit;
            _logger.LogDebug("KeyboardNavigationBehavior attached to DataGrid");
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            AssociatedObject.BeginningEdit -= OnBeginningEdit;
            _logger.LogDebug("KeyboardNavigationBehavior detached from DataGrid");
        }

        private void OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            try
            {
                if (e.Row.Item is DataGridRowModel row &&
                    e.Column.Header is string columnName)
                {
                    var cell = row.GetCell(columnName);
                    if (cell != null)
                    {
                        if (cell.OriginalValue == null)
                        {
                            cell.StartEditing();
                            _logger.LogDebug("Started editing cell: {ColumnName} = '{Value}'", columnName, cell.Value);
                        }
                        else
                        {
                            cell.IsEditing = true;
                            _logger.LogDebug("Continuing edit for cell: {ColumnName}", columnName);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in OnBeginningEdit for column");
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.Tab:
                        HandleTabKey(e);
                        break;
                    case Key.Enter:
                        HandleEnterKey(e);
                        break;
                    case Key.Escape:
                        HandleEscapeKey(e);
                        break;
                    case Key.F2:
                        HandleF2Key(e);
                        break;
                    case Key.Delete:
                        HandleDeleteKey(e);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in KeyboardNavigationBehavior key handling for key: {Key}", e.Key);
            }
        }

        private void HandleTabKey(KeyEventArgs e)
        {
            try
            {
                if (AssociatedObject.IsInEditingMode())
                {
                    CommitCurrentCellChanges();
                    AssociatedObject.CommitEdit(DataGridEditingUnit.Cell, true);
                }

                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    if (NavigationService != null)
                    {
                        if (Keyboard.Modifiers == ModifierKeys.Shift)
                            NavigationService.MoveToPreviousCell();
                        else
                            NavigationService.MoveToNextCell();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);

                e.Handled = true;
                _logger.LogDebug("TAB navigation executed with commit");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in HandleTabKey");
            }
        }

        private void HandleEnterKey(KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    return;
                }

                if (AssociatedObject.IsInEditingMode())
                {
                    CommitCurrentCellChanges();
                    AssociatedObject.CommitEdit(DataGridEditingUnit.Cell, true);

                    if (NavigationService != null)
                    {
                        Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            NavigationService.MoveToNextRow();
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }
                else
                {
                    AssociatedObject.BeginEdit();
                }

                e.Handled = true;
                _logger.LogDebug("ENTER navigation executed");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in HandleEnterKey");
            }
        }

        private void HandleEscapeKey(KeyEventArgs e)
        {
            try
            {
                if (AssociatedObject.IsInEditingMode() && AssociatedObject.CurrentCell.IsValid)
                {
                    var currentCell = AssociatedObject.CurrentCell;

                    if (currentCell.Item is DataGridRowModel row &&
                        currentCell.Column?.Header is string columnName)
                    {
                        var cell = row.GetCell(columnName);
                        if (cell != null)
                        {
                            _logger.LogDebug("ESC - canceling editing for {ColumnName}", columnName);

                            cell.CancelEditing();
                            AssociatedObject.CancelEdit(DataGridEditingUnit.Cell);

                            Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                try
                                {
                                    AssociatedObject.Focus();
                                    AssociatedObject.CurrentCell = currentCell;
                                    AssociatedObject.SelectedCells.Clear();
                                    AssociatedObject.SelectedCells.Add(currentCell);

                                    _logger.LogDebug("ESC completed - changes cancelled for {ColumnName}", columnName);
                                }
                                catch (System.Exception ex)
                                {
                                    _logger.LogError(ex, "Error in ESC post-processing");
                                }
                            }), System.Windows.Threading.DispatcherPriority.Input);
                        }
                    }

                    e.Handled = true;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in HandleEscapeKey");
            }
        }

        private void HandleDeleteKey(KeyEventArgs e)
        {
            try
            {
                if (AssociatedObject.CurrentCell.IsValid &&
                    AssociatedObject.CurrentCell.Item != null &&
                    AssociatedObject.CurrentCell.Column != null &&
                    !AssociatedObject.IsInEditingMode())
                {
                    var currentItem = AssociatedObject.CurrentCell.Item;
                    var currentColumn = AssociatedObject.CurrentCell.Column;

                    if (currentItem is DataGridRowModel row &&
                        currentColumn.Header is string columnName &&
                        !IsSpecialColumn(columnName))
                    {
                        var cell = row.GetCell(columnName);
                        if (cell != null)
                        {
                            cell.StartEditing();
                            row.SetValue(columnName, null);
                            cell.CommitChanges();

                            e.Handled = true;
                            _logger.LogDebug("DELETE - cleared value in {ColumnName}", columnName);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in HandleDeleteKey");
            }
        }

        private void HandleF2Key(KeyEventArgs e)
        {
            try
            {
                if (!AssociatedObject.IsInEditingMode())
                {
                    AssociatedObject.BeginEdit();
                    e.Handled = true;
                    _logger.LogDebug("F2 - started editing current cell");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in HandleF2Key");
            }
        }

        private bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        private void CommitCurrentCellChanges()
        {
            try
            {
                if (AssociatedObject.CurrentCell.IsValid &&
                    AssociatedObject.CurrentCell.Item is DataGridRowModel row &&
                    AssociatedObject.CurrentCell.Column?.Header is string columnName)
                {
                    var cell = row.GetCell(columnName);
                    if (cell != null)
                    {
                        cell.CommitChanges();
                        _logger.LogDebug("Committed changes for cell: {ColumnName}", columnName);
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error in CommitCurrentCellChanges");
            }
        }
    }

    public static class DataGridExtensions
    {
        public static bool IsInEditingMode(this DataGrid dataGrid)
        {
            return dataGrid.CurrentCell.IsValid &&
                   dataGrid.CurrentColumn != null &&
                   dataGrid.CurrentColumn.GetCellContent(dataGrid.CurrentItem) is FrameworkElement element &&
                   element.IsKeyboardFocusWithin;
        }
    }
}