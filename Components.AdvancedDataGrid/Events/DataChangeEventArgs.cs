// ===========================================
//Events/DataChangedEventArgs.cs
// ===========================================
using System;

namespace Components.AdvancedDataGrid.Events
{
    public class DataChangeEventArgs : EventArgs
    {
        public DataChangeType ChangeType { get; set; }
        public object? ChangedData { get; set; }
        public string? ColumnName { get; set; }
        public int RowIndex { get; set; }
    }

    public enum DataChangeType
    {
        LoadData,
        ClearData,
        CellValueChanged,
        RemoveRows,
        RemoveEmptyRows,
        AddRow
    }
}