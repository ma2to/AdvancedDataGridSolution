// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Events/ValidationCompletedEventArgs.cs
// ===========================================
using RpaWpfComponents.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWpfComponents.AdvancedDataGrid.Events
{
    public class ValidationCompletedEventArgs : EventArgs
    {
        public DataGridRowModel? Row { get; set; }
        public DataGridCellModel? Cell { get; set; }
        public List<ValidationResultModel> Results { get; set; } = new();
        public bool IsValid => Results.All(r => r.IsValid);
    }
}