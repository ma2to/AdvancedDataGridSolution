// RpaWpfComponents/AdvancedDataGrid/Models/ColumnDefinitionModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RpaWpfComponents.AdvancedDataGrid.Models
{
    public class ColumnDefinitionModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private Type _dataType = typeof(string);
        private double _minWidth = 80;
        private double _maxWidth = 300;
        private bool _allowResize = true;
        private bool _allowSort = true;
        private bool _isReadOnly = false;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value ?? string.Empty);
        }

        public Type DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        public double MinWidth
        {
            get => _minWidth;
            set => SetProperty(ref _minWidth, value);
        }

        public double MaxWidth
        {
            get => _maxWidth;
            set => SetProperty(ref _maxWidth, value);
        }

        public bool AllowResize
        {
            get => _allowResize;
            set => SetProperty(ref _allowResize, value);
        }

        public bool AllowSort
        {
            get => _allowSort;
            set => SetProperty(ref _allowSort, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        public bool IsSpecialColumn => Name == "DeleteAction" || Name == "ValidAlerts";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}