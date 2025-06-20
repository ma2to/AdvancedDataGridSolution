// Behaviors/KeyboardNavigationBehavior.cs - OPRAVENÝ
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Components.AdvancedDataGrid.Services.Interfaces;

namespace Components.AdvancedDataGrid.Behaviors
{
    public class KeyboardNavigationBehavior : Behavior<DataGrid>
    {
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
            AssociatedObject.CellEditEnding += OnCellEditEnding;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            AssociatedObject.CellEditEnding -= OnCellEditEnding;
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
                System.Diagnostics.Debug.WriteLine($"KeyboardNavigationBehavior error: {ex.Message}");
            }
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                // If edit was cancelled, don't commit changes
                if (e.EditAction == DataGridEditAction.Cancel)
                {
                    return;
                }

                // Move to next cell after commit
                if (NavigationService != null && e.EditAction == DataGridEditAction.Commit)
                {
                    // Small delay to allow commit to complete
                    Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        NavigationService.MoveToNextCell();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardNavigationBehavior CellEditEnding error: {ex.Message}");
            }
        }

        private void HandleTabKey(KeyEventArgs e)
        {
            try
            {
                // Commit current edit first
                if (AssociatedObject.IsInEditingMode())
                {
                    AssociatedObject.CommitEdit(DataGridEditingUnit.Cell, true);
                }

                if (NavigationService != null)
                {
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                        NavigationService.MoveToPreviousCell();
                    else
                        NavigationService.MoveToNextCell();
                }

                e.Handled = true;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardNavigationBehavior HandleTabKey error: {ex.Message}");
            }
        }

        private void HandleEnterKey(KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    // Shift+Enter = nový riadok v TextBoxe (default behavior)
                    return;
                }

                // Enter = commit current edit and move to next row
                if (AssociatedObject.IsInEditingMode())
                {
                    AssociatedObject.CommitEdit(DataGridEditingUnit.Cell, true);

                    // Move to next row, same column
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
                    // Start editing current cell
                    AssociatedObject.BeginEdit();
                }

                e.Handled = true;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardNavigationBehavior HandleEnterKey error: {ex.Message}");
            }
        }

        private void HandleEscapeKey(KeyEventArgs e)
        {
            try
            {
                // ESC = cancel current edit - OPRAVENÉ
                if (AssociatedObject.IsInEditingMode())
                {
                    // Zruš edit a vráť pôvodnú hodnotu
                    AssociatedObject.CancelEdit(DataGridEditingUnit.Cell);
                    e.Handled = true;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardNavigationBehavior HandleEscapeKey error: {ex.Message}");
            }
        }

        private void HandleDeleteKey(KeyEventArgs e)
        {
            try
            {
                // Delete = vymaž obsah aktívnej bunky
                if (AssociatedObject.CurrentCell.IsValid &&
                    AssociatedObject.CurrentCell.Item != null &&
                    AssociatedObject.CurrentCell.Column != null &&
                    !AssociatedObject.IsInEditingMode())
                {
                    var currentItem = AssociatedObject.CurrentCell.Item;
                    var currentColumn = AssociatedObject.CurrentCell.Column;

                    if (currentItem is Components.AdvancedDataGrid.Models.DataGridRowModel row &&
                        currentColumn.Header is string columnName &&
                        !IsSpecialColumn(columnName))
                    {
                        row.SetValue(columnName, null);
                        e.Handled = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardNavigationBehavior HandleDeleteKey error: {ex.Message}");
            }
        }

        private bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        private void HandleF2Key(KeyEventArgs e)
        {
            try
            {
                // F2 = start editing current cell or focus Mirror Editor
                if (!AssociatedObject.IsInEditingMode())
                {
                    AssociatedObject.BeginEdit();
                    e.Handled = true;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyboardNavigationBehavior HandleF2Key error: {ex.Message}");
            }
        }
    }

    // Extension method to check if DataGrid is in editing mode
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