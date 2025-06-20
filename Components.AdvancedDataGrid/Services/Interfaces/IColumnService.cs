// ===========================================
// Services/Interfaces/IColumnService.cs
// ===========================================
using Components.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using Components.AdvancedDataGrid.Events;

namespace Components.AdvancedDataGrid.Services.Interfaces
{
    public interface IColumnService
    {
        List<ColumnDefinitionModel> ProcessColumnDefinitions(List<ColumnDefinitionModel> columns);
        string GenerateUniqueColumnName(string baseName, List<string> existingNames);
        ColumnDefinitionModel CreateDeleteActionColumn();
        ColumnDefinitionModel CreateValidAlertsColumn();
        bool IsSpecialColumn(string columnName);

        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}