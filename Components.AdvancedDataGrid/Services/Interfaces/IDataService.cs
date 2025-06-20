// ===========================================
// Services/Interfaces/IDataService.cs
// ===========================================
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Components.AdvancedDataGrid.Services.Interfaces
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

        // OPRAVENÉ - použije DataChangeEventArgs namiesto DataChangedEventArgs
        event EventHandler<DataChangeEventArgs> DataChanged;
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}