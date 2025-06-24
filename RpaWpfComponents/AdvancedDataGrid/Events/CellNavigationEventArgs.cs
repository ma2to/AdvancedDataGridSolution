// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Events/CellNavigationEventArgs.cs
// ===========================================

using RpaWpfComponents.AdvancedDataGrid.Models;
using System;

namespace RpaWpfComponents.AdvancedDataGrid.Events
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