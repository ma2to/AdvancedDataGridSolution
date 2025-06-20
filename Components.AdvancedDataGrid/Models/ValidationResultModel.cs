// ===========================================
// Models/ValidationResultModel.cs
// ===========================================
using System.Collections.Generic;

namespace Components.AdvancedDataGrid.Models
{
    public class ValidationResultModel
    {
        public bool IsValid { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
        public string ColumnName { get; set; }
        public int RowIndex { get; set; }

        public ValidationResultModel(bool isValid = true)
        {
            IsValid = isValid;
        }

        public static ValidationResultModel Success() => new ValidationResultModel(true);

        public static ValidationResultModel Failure(string errorMessage)
        {
            return new ValidationResultModel(false) { ErrorMessages = new List<string> { errorMessage } };
        }

        public static ValidationResultModel Failure(List<string> errorMessages)
        {
            return new ValidationResultModel(false) { ErrorMessages = errorMessages };
        }
    }
}