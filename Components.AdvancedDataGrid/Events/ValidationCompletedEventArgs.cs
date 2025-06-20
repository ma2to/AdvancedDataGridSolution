// ===========================================
// Events/ValidationErrorEventArgs.cs
// ===========================================
using System;
using System.Collections.Generic;
using System.Linq;
using Components.AdvancedDataGrid.Models;

namespace Components.AdvancedDataGrid.Events
{
    public class ValidationCompletedEventArgs : EventArgs
    {
        public DataGridRowModel? Row { get; set; }
        public DataGridCellModel? Cell { get; set; }
        public List<ValidationResultModel> Results { get; set; } = new();
        public bool IsValid => Results.All(r => r.IsValid);
    }
}