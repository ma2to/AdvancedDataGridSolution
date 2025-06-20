// ===========================================
// Events/CellNavigationEventArgs.cs - NOVÝ SÚBOR
// ===========================================

using System;
using Components.AdvancedDataGrid.Models;

namespace Components.AdvancedDataGrid.Events
{
    public class CellNavigationEventArgs : EventArgs
    {
        public int OldRowIndex { get; set; }
        public int OldColumnIndex { get; set; }
        public int NewRowIndex { get; set; }
        public int NewColumnIndex { get; set; }
        public DataGridCellModel? OldCell { get; set; }
        public DataGridCellModel? NewCell { get; set; }
    }
}