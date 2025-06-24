// ============================================
// RpaWpfComponents/SmartListBox/Models/SmartListBoxItem.cs
// ============================================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace RpaWpfComponents.SmartListBox.Models
{
    /// <summary>
    /// Model pre položku v SmartListBox
    /// </summary>
    public class SmartListBoxItem : INotifyPropertyChanged
    {
        private object? _originalValue;
        private string _displayText = string.Empty;
        private bool _isSelected;
        private SmartDataType _dataType;

        /// <summary>
        /// Originálna hodnota položky
        /// </summary>
        public object? OriginalValue
        {
            get => _originalValue;
            set
            {
                if (SetProperty(ref _originalValue, value))
                {
                    ProcessValue();
                }
            }
        }

        /// <summary>
        /// Text na zobrazenie
        /// </summary>
        public string DisplayText
        {
            get => _displayText;
            private set => SetProperty(ref _displayText, value);
        }

        /// <summary>
        /// Či je položka selektnutá
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Typ dát položky
        /// </summary>
        public SmartDataType DataType
        {
            get => _dataType;
            private set => SetProperty(ref _dataType, value);
        }

        /// <summary>
        /// Jedinečný identifikátor položky
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        public SmartListBoxItem(object? value)
        {
            OriginalValue = value;
        }

        /// <summary>
        /// Spracuje hodnotu a nastaví DisplayText a DataType
        /// </summary>
        private void ProcessValue()
        {
            if (OriginalValue == null)
            {
                DisplayText = "(prázdne)";
                DataType = SmartDataType.Empty;
                return;
            }

            var valueString = OriginalValue.ToString();

            // Detekcia typu a spracovania
            if (IsFilePath(valueString))
            {
                DisplayText = Path.GetFileName(valueString);
                DataType = SmartDataType.FilePath;
            }
            else if (IsDateTime(OriginalValue))
            {
                DisplayText = ((DateTime)OriginalValue).ToString("dd.MM.yyyy HH:mm");
                DataType = SmartDataType.DateTime;
            }
            else if (IsNumber(OriginalValue))
            {
                DisplayText = OriginalValue.ToString() ?? string.Empty;
                DataType = SmartDataType.Number;
            }
            else
            {
                DisplayText = valueString ?? string.Empty;
                DataType = SmartDataType.Text;
            }
        }

        private bool IsFilePath(string? value)
        {
            try
            {
                return !string.IsNullOrEmpty(value) &&
                       (value.Contains("\\") || value.Contains("/")) &&
                       Path.HasExtension(value);
            }
            catch
            {
                return false;
            }
        }

        private bool IsDateTime(object value)
        {
            return value is DateTime || value is DateTimeOffset;
        }

        private bool IsNumber(object value)
        {
            return value is int || value is long || value is float || value is double ||
                   value is decimal || value is byte || value is short;
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

    /// <summary>
    /// Typ dát v SmartListBox
    /// </summary>
    public enum SmartDataType
    {
        Text,
        Number,
        DateTime,
        FilePath,
        Empty
    }

    /// <summary>
    /// Režim selekcie v SmartListBox
    /// </summary>
    public enum SelectionMode
    {
        /// <summary>
        /// Možno vybrať len jeden item
        /// </summary>
        Single,
        /// <summary>
        /// Možno vybrať viac itemov
        /// </summary>
        Multiple
    }
}