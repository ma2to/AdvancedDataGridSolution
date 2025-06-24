// RpaWpfComponents/AdvancedDataGrid/Services/Interfaces/IValidationService.cs
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Interfaces
{
    public interface IValidationService
    {
        Task<ValidationResultModel> ValidateCellAsync(DataGridCellModel cell, DataGridRowModel row);
        Task<List<ValidationResultModel>> ValidateRowAsync(DataGridRowModel row);
        Task<List<ValidationResultModel>> ValidateAllRowsAsync(IEnumerable<DataGridRowModel> rows);

        void AddValidationRule(ValidationRuleModel rule);
        void RemoveValidationRule(string columnName, string ruleName);
        void ClearValidationRules(string? columnName = null);

        List<ValidationRuleModel> GetValidationRules(string columnName);
        bool HasValidationRules(string columnName);
        int GetTotalRuleCount();

        event EventHandler<ValidationCompletedEventArgs> ValidationCompleted;
        event EventHandler<ComponentErrorEventArgs> ValidationErrorOccurred;
    }
}