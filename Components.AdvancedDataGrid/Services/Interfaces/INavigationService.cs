// ===========================================
// Services/Interfaces/INavigationService.cs
// ===========================================
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Events;
using System;
using System.Collections.Generic;

namespace Components.AdvancedDataGrid.Services.Interfaces
{
    public interface INavigationService
    {
        void Initialize(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns);

        void MoveToNextCell();
        void MoveToPreviousCell();
        void MoveToNextRow();
        void MoveToPreviousRow();
        void MoveToCell(int rowIndex, int columnIndex);

        DataGridCellModel? CurrentCell { get; }
        int CurrentRowIndex { get; }
        int CurrentColumnIndex { get; }

        event EventHandler<CellNavigationEventArgs>? CellChanged;
        event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
    }
}