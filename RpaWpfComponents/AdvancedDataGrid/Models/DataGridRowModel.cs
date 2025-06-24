// RpaWpfComponents/AdvancedDataGrid/Models/DataGridRowModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RpaWpfComponents.AdvancedDataGrid.Models
{
    public class DataGridRowModel : INotifyPropertyChanged
    {
        private Dictionary<string, DataGridCellModel> _cells = new Dictionary<string, DataGridCellModel>();
        private bool _hasValidationErrors;
        private bool _isEmpty = true;

        public Dictionary<string, DataGridCellModel> Cells
        {
            get => _cells;
            set => SetProperty(ref _cells, value ?? new Dictionary<string, DataGridCellModel>());
        }

        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            private set => SetProperty(ref _isEmpty, value);
        }

        public string ValidationErrorsText
        {
            get
            {
                var errors = new List<string>();
                foreach (var cell in Cells.Values.Where(c => c.HasValidationError))
                {
                    errors.Add($"{cell.ColumnName}: {cell.ValidationErrorsText}");
                }
                return string.Join("; ", errors);
            }
        }

        public DataGridCellModel? GetCell(string columnName)
        {
            return Cells.TryGetValue(columnName, out var cell) ? cell : null;
        }

        public T GetValue<T>(string columnName)
        {
            var cell = GetCell(columnName);
            if (cell == null)
            {
                if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                {
                    return (T)Activator.CreateInstance(typeof(T))!;
                }
                return default(T)!;
            }
            return cell.GetValue<T>();
        }

        public void SetValue(string columnName, object? value)
        {
            if (Cells.TryGetValue(columnName, out var cell))
            {
                cell.Value = value;
                UpdateEmptyStatus();
            }
        }

        public void AddCell(string columnName, DataGridCellModel cell)
        {
            Cells[columnName] = cell;
            cell.ValueChanged += (s, e) => UpdateEmptyStatus();
            UpdateEmptyStatus();
        }

        public void UpdateValidationStatus()
        {
            HasValidationErrors = Cells.Values.Any(c => c.HasValidationError);
            OnPropertyChanged(nameof(ValidationErrorsText));
        }

        public void UpdateEmptyStatusAfterLoading()
        {
            try
            {
                UpdateEmptyStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UpdateEmptyStatusAfterLoading error: {ex.Message}");
            }
        }

        private void UpdateEmptyStatus()
        {
            try
            {
                var dataCells = Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName));

                var wasEmpty = IsEmpty;
                IsEmpty = dataCells.All(c =>
                    c.Value == null ||
                    string.IsNullOrWhiteSpace(c.Value?.ToString())
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UpdateEmptyStatus error: {ex.Message}");
                IsEmpty = false;
            }
        }

        private bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}