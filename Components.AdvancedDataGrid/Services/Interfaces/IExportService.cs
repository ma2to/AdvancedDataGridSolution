// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Services/Interfaces/IExportService.cs
// ===========================================
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Interfaces
{
    public interface IExportService
    {
        Task<DataTable> ExportToDataTableAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns);
        Task<string> ExportToCsvAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns);
        Task<byte[]> ExportToExcelAsync(List<DataGridRowModel> rows, List<ColumnDefinitionModel> columns);

        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}