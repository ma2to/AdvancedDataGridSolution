// ===========================================
// Models/DataGridCellModel.cs
// ===========================================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Components.AdvancedDataGrid.Models
{
    public class DataGridCellModel : INotifyPropertyChanged
    {
        private object? _value;
        private bool _hasValidationError;
        private List<string> _validationErrors = new();
        private bool _isSelected;
        private bool _isEditing;

        public string ColumnName { get; set; } = string.Empty;
        public Type DataType { get; set; } = typeof(string);

        public object? Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    OnValueChanged();
                }
            }
        }

        public bool HasValidationError
        {
            get => _hasValidationError;
            set => SetProperty(ref _hasValidationError, value);
        }

        public List<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value ?? new List<string>());
        }

        public string ValidationErrorsText => string.Join("; ", ValidationErrors);

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public T GetValue<T>()
        {
            try
            {
                if (Value == null)
                {
                    // Pre reference types vráti null, pre value types vráti default hodnotu
                    if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                    {
                        return (T)Activator.CreateInstance(typeof(T))!;
                    }
                    return default(T)!;
                }

                return (T)Convert.ChangeType(Value, typeof(T));
            }
            catch
            {
                // Pri chybe vráti default hodnotu
                if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                {
                    return (T)Activator.CreateInstance(typeof(T))!;
                }
                return default(T)!;
            }
        }

        public void SetValidationErrors(List<string>? errors)
        {
            ValidationErrors = errors ?? new List<string>();
            HasValidationError = ValidationErrors.Count > 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? ValueChanged;

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

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}