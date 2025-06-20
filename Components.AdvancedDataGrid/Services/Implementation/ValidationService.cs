// Services/Implementation/ValidationService.cs - VYLEPŠENÝ
using Components.AdvancedDataGrid.Events;
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Components.AdvancedDataGrid.Services.Implementation
{
    public class ValidationService : IValidationService
    {
        private readonly Dictionary<string, List<ValidationRuleModel>> _validationRules = new();

        public event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;
        public event EventHandler<ComponentErrorEventArgs>? ValidationErrorOccurred;

        public async Task<ValidationResultModel> ValidateCellAsync(DataGridCellModel cell, DataGridRowModel row)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var result = new ValidationResultModel(true)
                    {
                        ColumnName = cell.ColumnName
                    };

                    if (!_validationRules.ContainsKey(cell.ColumnName))
                        return result;

                    var rules = _validationRules[cell.ColumnName]
                        .Where(r => r.ShouldApply(row))
                        .OrderByDescending(r => r.Priority);

                    var errorMessages = new List<string>();

                    foreach (var rule in rules)
                    {
                        try
                        {
                            if (!rule.Validate(cell.Value, row))
                            {
                                errorMessages.Add(rule.ErrorMessage);
                                result.IsValid = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Validation rule error for {rule.RuleName}: {ex.Message}");
                            errorMessages.Add($"Validation error: {ex.Message}");
                            result.IsValid = false;
                        }
                    }

                    result.ErrorMessages = errorMessages;
                    cell.SetValidationErrors(errorMessages);

                    return result;
                });
            }
            catch (Exception ex)
            {
                OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellAsync"));
                return ValidationResultModel.Failure($"Chyba pri validácii: {ex.Message}");
            }
        }

        public async Task<List<ValidationResultModel>> ValidateRowAsync(DataGridRowModel row)
        {
            try
            {
                var results = new List<ValidationResultModel>();

                // Validate each cell that has validation rules
                var cellsToValidate = row.Cells.Values
                    .Where(c => !IsSpecialColumn(c.ColumnName) && _validationRules.ContainsKey(c.ColumnName))
                    .ToList();

                foreach (var cell in cellsToValidate)
                {
                    var result = await ValidateCellAsync(cell, row);
                    results.Add(result);
                }

                // Update row validation status
                row.UpdateValidationStatus();

                // Update ValidAlerts column if it exists
                var validAlertsCell = row.GetCell("ValidAlerts");
                if (validAlertsCell != null)
                {
                    validAlertsCell.Value = row.ValidationErrorsText;
                }

                OnValidationCompleted(new ValidationCompletedEventArgs
                {
                    Row = row,
                    Results = results
                });

                return results;
            }
            catch (Exception ex)
            {
                OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateRowAsync"));
                return new List<ValidationResultModel>();
            }
        }

        public async Task<List<ValidationResultModel>> ValidateAllRowsAsync(IEnumerable<DataGridRowModel> rows)
        {
            try
            {
                var allResults = new List<ValidationResultModel>();
                var dataRows = rows.Where(r => !r.IsEmpty).ToList();

                if (dataRows.Count == 0)
                {
                    return allResults;
                }

                // Process rows in batches for better performance and progress reporting
                const int batchSize = 10;
                var totalRows = dataRows.Count;
                var processedRows = 0;

                for (int i = 0; i < dataRows.Count; i += batchSize)
                {
                    var batch = dataRows.Skip(i).Take(batchSize).ToList();

                    var batchTasks = batch.Select(async row =>
                    {
                        try
                        {
                            return await ValidateRowAsync(row);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error validating row: {ex.Message}");
                            return new List<ValidationResultModel>();
                        }
                    });

                    var batchResults = await Task.WhenAll(batchTasks);

                    foreach (var rowResults in batchResults)
                    {
                        allResults.AddRange(rowResults);
                    }

                    processedRows += batch.Count;

                    // Report progress could be added here if needed
                    System.Diagnostics.Debug.WriteLine($"Validated {processedRows}/{totalRows} rows");
                }

                return allResults;
            }
            catch (Exception ex)
            {
                OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return new List<ValidationResultModel>();
            }
        }

        public void AddValidationRule(ValidationRuleModel rule)
        {
            try
            {
                if (rule == null)
                    throw new ArgumentNullException(nameof(rule));

                if (string.IsNullOrWhiteSpace(rule.ColumnName))
                    throw new ArgumentException("ColumnName cannot be null or empty", nameof(rule));

                if (!_validationRules.ContainsKey(rule.ColumnName))
                {
                    _validationRules[rule.ColumnName] = new List<ValidationRuleModel>();
                }

                // Remove existing rule with same name if it exists
                _validationRules[rule.ColumnName].RemoveAll(r => r.RuleName == rule.RuleName);

                // Add new rule
                _validationRules[rule.ColumnName].Add(rule);

                System.Diagnostics.Debug.WriteLine($"Added validation rule '{rule.RuleName}' for column '{rule.ColumnName}'");
            }
            catch (Exception ex)
            {
                OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "AddValidationRule"));
            }
        }

        public void RemoveValidationRule(string columnName, string ruleName)
        {
            try
            {
                if (_validationRules.ContainsKey(columnName))
                {
                    var removedCount = _validationRules[columnName].RemoveAll(r => r.RuleName == ruleName);
                    System.Diagnostics.Debug.WriteLine($"Removed {removedCount} validation rule(s) '{ruleName}' from column '{columnName}'");
                }
            }
            catch (Exception ex)
            {
                OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveValidationRule"));
            }
        }

        public void ClearValidationRules(string? columnName = null)
        {
            try
            {
                if (columnName == null)
                {
                    var totalRules = _validationRules.Values.Sum(rules => rules.Count);
                    _validationRules.Clear();
                    System.Diagnostics.Debug.WriteLine($"Cleared all {totalRules} validation rules");
                }
                else if (_validationRules.ContainsKey(columnName))
                {
                    var ruleCount = _validationRules[columnName].Count;
                    _validationRules[columnName].Clear();
                    System.Diagnostics.Debug.WriteLine($"Cleared {ruleCount} validation rules from column '{columnName}'");
                }
            }
            catch (Exception ex)
            {
                OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ClearValidationRules"));
            }
        }

        public List<ValidationRuleModel> GetValidationRules(string columnName)
        {
            try
            {
                return _validationRules.TryGetValue(columnName, out var rules)
                    ? new List<ValidationRuleModel>(rules)
                    : new List<ValidationRuleModel>();
            }
            catch (Exception ex)
            {
                OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "GetValidationRules"));
                return new List<ValidationRuleModel>();
            }
        }

        public bool HasValidationRules(string columnName)
        {
            return _validationRules.ContainsKey(columnName) && _validationRules[columnName].Count > 0;
        }

        public int GetTotalRuleCount()
        {
            return _validationRules.Values.Sum(rules => rules.Count);
        }

        private bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        protected virtual void OnValidationCompleted(ValidationCompletedEventArgs e)
        {
            ValidationCompleted?.Invoke(this, e);
        }

        protected virtual void OnValidationErrorOccurred(ComponentErrorEventArgs e)
        {
            ValidationErrorOccurred?.Invoke(this, e);
        }
    }
}