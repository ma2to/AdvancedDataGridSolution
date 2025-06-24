// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Services/Interfaces/IDataService.cs
// ===========================================
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Interfaces
{
    public interface IDataService
    {
        void Initialize(List<ColumnDefinitionModel> columns);

        Task LoadDataAsync(DataTable dataTable);
        Task LoadDataAsync(List<Dictionary<string, object>> data);
        Task<DataTable> ExportDataAsync();
        Task ClearAllDataAsync();
        Task<bool> ValidateAllRowsAsync();
        Task RemoveRowsByConditionAsync(string columnName, Func<object, bool> condition);
        Task RemoveEmptyRowsAsync();

        event EventHandler<DataChangeEventArgs> DataChanged;
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}