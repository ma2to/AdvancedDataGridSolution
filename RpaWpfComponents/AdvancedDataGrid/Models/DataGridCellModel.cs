// RpaWpfComponents/AdvancedDataGrid/Models/DataGridCellModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RpaWpfComponents.AdvancedDataGrid.Models
{
    public class DataGridCellModel : INotifyPropertyChanged
    {
        private object? _value;
        private object? _originalValue;
        private bool _hasValidationError;
        private List<string> _validationErrors = new();
        private bool _isSelected;
        private bool _isEditing;
        private bool _hasUnsavedChanges;

        public string ColumnName { get; set; } = string.Empty;
        public Type DataType { get; set; } = typeof(string);

        public object? Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    if (_originalValue == null && !_isEditing)
                    {
                        _originalValue = value;
                    }

                    UpdateUnsavedChangesStatus();
                    OnValueChanged();
                }
            }
        }

        public void SetValueWithoutValidation(object? value)
        {
            _value = value;
            _originalValue = value;
            _hasUnsavedChanges = false;

            OnPropertyChanged(nameof(Value));
        }

        public object? OriginalValue
        {
            get => _originalValue;
            private set => SetProperty(ref _originalValue, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set => SetProperty(ref _hasUnsavedChanges, value);
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
            set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    if (_isEditing)
                    {
                        StartEditing();
                    }
                    else
                    {
                        EndEditing();
                    }
                }
            }
        }

        public void StartEditing()
        {
            OriginalValue = _value;
            HasUnsavedChanges = false;
        }

        public void EndEditing()
        {
            OriginalValue = _value;
            HasUnsavedChanges = false;
        }

        public void CancelEditing()
        {
            if (HasUnsavedChanges && OriginalValue != _value)
            {
                Value = OriginalValue;

                if (HasValidationError)
                {
                    SetValidationErrors(new List<string>());
                }
            }

            HasUnsavedChanges = false;
        }

        public void CommitChanges()
        {
            if (HasUnsavedChanges)
            {
                EndEditing();
            }
        }

        public T GetValue<T>()
        {
            try
            {
                if (Value == null)
                {
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

        private void UpdateUnsavedChangesStatus()
        {
            HasUnsavedChanges = IsEditing && !AreValuesEqual(_value, _originalValue);
        }

        private bool AreValuesEqual(object? value1, object? value2)
        {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;

            if (value1 is string str1 && value2 is string str2)
            {
                return string.Equals(str1?.Trim(), str2?.Trim());
            }

            return value1.Equals(value2);
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