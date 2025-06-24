// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Models/ValidationRuleModel.cs
// ===========================================
using System;

namespace RpaWpfComponents.AdvancedDataGrid.Models
{
    public class ValidationRuleModel
    {
        public string ColumnName { get; set; }
        public Func<object, DataGridRowModel, bool> ValidationFunction { get; set; }
        public string ErrorMessage { get; set; }
        public Func<DataGridRowModel, bool> ApplyCondition { get; set; } = _ => true;
        public int Priority { get; set; } = 0;
        public string RuleName { get; set; }

        public ValidationRuleModel()
        {
            RuleName = Guid.NewGuid().ToString();
        }

        public bool ShouldApply(DataGridRowModel row)
        {
            try
            {
                return ApplyCondition?.Invoke(row) ?? true;
            }
            catch
            {
                return true;
            }
        }

        public bool Validate(object value, DataGridRowModel row)
        {
            try
            {
                if (!ShouldApply(row))
                    return true;

                return ValidationFunction?.Invoke(value, row) ?? true;
            }
            catch
            {
                return false;
            }
        }
    }
}