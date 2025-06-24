// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Helpers/ValidationHelper.cs
// ===========================================
using RpaWpfComponents.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWpfComponents.AdvancedDataGrid.Helpers
{
    public static class ValidationHelper
    {
        public static ValidationRuleModel CreateRequiredRule(string columnName, string errorMessage = null)
        {
            return new ValidationRuleModel
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                ErrorMessage = errorMessage ?? $"{columnName} je povinné pole",
                RuleName = $"{columnName}_Required"
            };
        }

        public static ValidationRuleModel CreateLengthRule(string columnName, int minLength, int maxLength = int.MaxValue, string errorMessage = null)
        {
            return new ValidationRuleModel
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) =>
                {
                    var text = value?.ToString() ?? "";
                    return text.Length >= minLength && text.Length <= maxLength;
                },
                ErrorMessage = errorMessage ?? $"{columnName} musí mať dĺžku medzi {minLength} a {maxLength} znakmi",
                RuleName = $"{columnName}_Length"
            };
        }

        public static ValidationRuleModel CreateNumericRule(string columnName, string errorMessage = null)
        {
            return new ValidationRuleModel
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        return true;

                    return double.TryParse(value.ToString(), out _);
                },
                ErrorMessage = errorMessage ?? $"{columnName} musí byť číslo",
                RuleName = $"{columnName}_Numeric"
            };
        }

        public static ValidationRuleModel CreateRangeRule(string columnName, double min, double max, string errorMessage = null)
        {
            return new ValidationRuleModel
            {
                ColumnName = columnName,
                ValidationFunction = (value, row) =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                        return true;

                    if (double.TryParse(value.ToString(), out double numValue))
                    {
                        return numValue >= min && numValue <= max;
                    }

                    return false;
                },
                ErrorMessage = errorMessage ?? $"{columnName} musí byť medzi {min} a {max}",
                RuleName = $"{columnName}_Range"
            };
        }

        public static ValidationRuleModel CreateConditionalRule(string columnName,
            Func<object, DataGridRowModel, bool> validationFunction,
            Func<DataGridRowModel, bool> condition,
            string errorMessage,
            string ruleName = null)
        {
            return new ValidationRuleModel
            {
                ColumnName = columnName,
                ValidationFunction = validationFunction,
                ApplyCondition = condition,
                ErrorMessage = errorMessage,
                RuleName = ruleName ?? $"{columnName}_Conditional_{Guid.NewGuid().ToString("N")[..8]}"
            };
        }
    }
}