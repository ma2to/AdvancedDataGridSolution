// Services/Implementation/NavigationService.cs - OPRAVENÝ
using Components.AdvancedDataGrid.Events;
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Components.AdvancedDataGrid.Services.Implementation
{
    public class NavigationService : INavigationService
    {
        private List<DataGridRowModel> _rows = new();
        private List<ColumnDefinitionModel> _columns = new();
        private int _currentRowIndex = -1;
        private int _currentColumnIndex = -1;
        private DataGridCellModel? _currentCell;

        public DataGridCellModel? CurrentCell
        {
            get => _currentCell;
            private set
            {
                if (_currentCell != value)
                {
                    _currentCell = value;
                }
            }
        }

        public int CurrentRowIndex => _currentRowIndex;
        public int CurrentColumnIndex => _currentColumnIndex;

        public event EventHandler<CellNavigationEventArgs>? CellChanged;
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        public void Initialize(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));

            // Reset current position
            _currentRowIndex = -1;
            _currentColumnIndex = -1;
            CurrentCell = null;

            // Set initial position if data exists
            if (_rows.Count > 0)
            {
                var editableColumns = GetEditableColumns();
                if (editableColumns.Count > 0)
                {
                    MoveToCell(0, 0);
                }
            }
        }

        public void MoveToNextCell()
        {
            try
            {
                var editableColumns = GetEditableColumns();
                if (editableColumns.Count == 0 || _rows.Count == 0) return;

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                // Nájdi nasledujúci editovateľný stĺpec
                var nextColumnIndex = _currentColumnIndex + 1;
                var nextRowIndex = _currentRowIndex;

                if (nextColumnIndex >= editableColumns.Count)
                {
                    // Prejdi na ďalší riadok
                    nextColumnIndex = 0;
                    nextRowIndex = _currentRowIndex + 1;
                    if (nextRowIndex >= _rows.Count)
                        nextRowIndex = 0; // Začni od začiatku
                }

                MoveToCell(nextRowIndex, nextColumnIndex);

                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToNextCell"));
            }
        }

        public void MoveToPreviousCell()
        {
            try
            {
                var editableColumns = GetEditableColumns();
                if (editableColumns.Count == 0 || _rows.Count == 0) return;

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                // Nájdi predchádzajúci editovateľný stĺpec
                var prevColumnIndex = _currentColumnIndex - 1;
                var prevRowIndex = _currentRowIndex;

                if (prevColumnIndex < 0)
                {
                    // Prejdi na predchádzajúci riadok
                    prevColumnIndex = editableColumns.Count - 1;
                    prevRowIndex = _currentRowIndex - 1;
                    if (prevRowIndex < 0)
                        prevRowIndex = _rows.Count - 1; // Prejdi na koniec
                }

                MoveToCell(prevRowIndex, prevColumnIndex);

                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToPreviousCell"));
            }
        }

        public void MoveToNextRow()
        {
            try
            {
                if (_rows.Count == 0) return;

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                var nextRowIndex = (_currentRowIndex + 1) % _rows.Count;
                MoveToCell(nextRowIndex, _currentColumnIndex);

                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToNextRow"));
            }
        }

        public void MoveToPreviousRow()
        {
            try
            {
                if (_rows.Count == 0) return;

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                var prevRowIndex = _currentRowIndex - 1;
                if (prevRowIndex < 0)
                    prevRowIndex = _rows.Count - 1;

                MoveToCell(prevRowIndex, _currentColumnIndex);

                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToPreviousRow"));
            }
        }

        public void MoveToCell(int rowIndex, int columnIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= _rows.Count) return;

                var editableColumns = GetEditableColumns();
                if (columnIndex < 0 || columnIndex >= editableColumns.Count) return;

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                _currentRowIndex = rowIndex;
                _currentColumnIndex = columnIndex;

                var columnName = editableColumns[columnIndex].Name;
                CurrentCell = _rows[rowIndex].GetCell(columnName);

                // Fire changed event only if position actually changed
                if (oldRowIndex != _currentRowIndex || oldColumnIndex != _currentColumnIndex)
                {
                    OnCellChanged(new CellNavigationEventArgs
                    {
                        OldRowIndex = oldRowIndex,
                        OldColumnIndex = oldColumnIndex,
                        NewRowIndex = _currentRowIndex,
                        NewColumnIndex = _currentColumnIndex,
                        OldCell = oldCell,
                        NewCell = CurrentCell
                    });
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToCell"));
            }
        }

        private List<ColumnDefinitionModel> GetEditableColumns()
        {
            return _columns.Where(c => !c.IsSpecialColumn).ToList();
        }

        protected virtual void OnCellChanged(CellNavigationEventArgs e)
        {
            CellChanged?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}