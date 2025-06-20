// ===========================================
// Services/Interfaces/IExportService.cs
// ===========================================
using Components.AdvancedDataGrid.Events;
using Components.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Components.AdvancedDataGrid.Services.Interfaces
{
    public interface IExportService
    {
        Task<DataTable> ExportToDataTableAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns);
        Task<string> ExportToCsvAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns);
        Task<byte[]> ExportToExcelAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns);

        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}