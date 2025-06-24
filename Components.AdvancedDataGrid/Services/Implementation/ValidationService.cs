// RpaWpfComponents/AdvancedDataGrid/Services/Implementation/ValidationService.cs
using RpaWpfComponents.AdvancedDataGrid.Events;
using RpaWpfComponents.AdvancedDataGrid.Models;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Implementation
{
    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        private readonly Dictionary<string, List<ValidationRuleModel>> _validationRules = new();

        public ValidationService(ILogger<ValidationService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ValidationService>.Instance;
        }

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

                    if (row.IsEmpty)
                    {
                        cell.SetValidationErrors(new List<string>());
                        _logger.LogTrace("Validation skipped for empty row, column: {ColumnName}", cell.ColumnName);
                        return result;
                    }

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
                                _logger.LogDebug("Validation rule '{RuleName}' failed for {ColumnName} = '{Value}'",
                                    rule.RuleName, cell.ColumnName, cell.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Validation rule '{RuleName}' threw exception for {ColumnName}",
                                rule.RuleName, cell.ColumnName);
                            errorMessages.Add($"Validation error: {ex.Message}");
                            result.IsValid = false;
                        }
                    }

                    result.ErrorMessages = errorMessages;
                    cell.SetValidationErrors(errorMessages);

                    if (errorMessages.Count > 0)
                    {
                        _logger.LogDebug("Validation failed for {ColumnName} = '{Value}': {Errors}",
                            cell.ColumnName, cell.Value, string.Join(", ", errorMessages));
                    }

                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cell {ColumnName}", cell?.ColumnName);
                OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellAsync"));
                return ValidationResultModel.Failure($"Chyba pri validácii: {ex.Message}");
            }
        }

        public async Task<List<ValidationResultModel>> ValidateRowAsync(DataGridRowModel row)
        {
            try
            {
                var results = new List<ValidationResultModel>();
                var validAlertsCell = row.GetCell("ValidAlerts");

                if (row.IsEmpty)
                {
                    foreach (var cell in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        cell.SetValidationErrors(new List<string>());
                    }

                    row.UpdateValidationStatus();

                    if (validAlertsCell != null)
                    {
                        validAlertsCell.Value = "";
                    }

                    _logger.LogTrace("Row validation skipped for empty row");

                    OnValidationCompleted(new ValidationCompletedEventArgs
                    {
                        Row = row,
                        Results = results
                    });

                    return results;
                }

                _logger.LogDebug("Validating row with {CellCount} cells", row.Cells.Count);

                var cellsToValidate = row.Cells.Values
                    .Where(c => !IsSpecialColumn(c.ColumnName) && _validationRules.ContainsKey(c.ColumnName))
                    .ToList();

                foreach (var cell in cellsToValidate)
                {
                    var result = await ValidateCellAsync(cell, row);
                    results.Add(result);
                }

                row.UpdateValidationStatus();

                if (validAlertsCell != null)
                {
                    validAlertsCell.Value = row.ValidationErrorsText;
                }

                var validCount = results.Count(r => r.IsValid);
                var invalidCount = results.Count(r => !r.IsValid);
                _logger.LogDebug("Row validation completed: {ValidCount} valid, {InvalidCount} invalid", validCount, invalidCount);

                OnValidationCompleted(new ValidationCompletedEventArgs
                {
                    Row = row,
                    Results = results
                });

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating row");
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
                    _logger.LogInformation("ValidateAllRows: No non-empty rows to validate");
                    return allResults;
                }

                _logger.LogInformation("Validating {RowCount} non-empty rows", dataRows.Count);

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
                            _logger.LogError(ex, "Error validating row in batch");
                            return new List<ValidationResultModel>();
                        }
                    });

                    var batchResults = await Task.WhenAll(batchTasks);

                    foreach (var rowResults in batchResults)
                    {
                        allResults.AddRange(rowResults);
                    }

                    processedRows += batch.Count;
                    _logger.LogDebug("Validated batch: {ProcessedRows}/{TotalRows} rows", processedRows, totalRows);
                }

                var validCount = allResults.Count(r => r.IsValid);
                var invalidCount = allResults.Count(r => !r.IsValid);
                _logger.LogInformation("ValidateAllRows completed: {ValidCount} valid, {InvalidCount} invalid results", validCount, invalidCount);

                return allResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating all rows");
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

                _validationRules[rule.ColumnName].RemoveAll(r => r.RuleName == rule.RuleName);
                _validationRules[rule.ColumnName].Add(rule);

                _logger.LogDebug("Added validation rule '{RuleName}' for column '{ColumnName}'", rule.RuleName, rule.ColumnName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding validation rule");
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
                    _logger.LogDebug("Removed {RemovedCount} validation rule(s) '{RuleName}' from column '{ColumnName}'",
                        removedCount, ruleName, columnName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing validation rule '{RuleName}' from column '{ColumnName}'", ruleName, columnName);
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
                    _logger.LogInformation("Cleared all {TotalRules} validation rules", totalRules);
                }
                else if (_validationRules.ContainsKey(columnName))
                {
                    var ruleCount = _validationRules[columnName].Count;
                    _validationRules[columnName].Clear();
                    _logger.LogDebug("Cleared {RuleCount} validation rules from column '{ColumnName}'", ruleCount, columnName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing validation rules for column: {ColumnName}", columnName ?? "ALL");
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
                _logger.LogError(ex, "Error getting validation rules for column: {ColumnName}", columnName);
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