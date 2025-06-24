// ===========================================
// RpaWpfComponents/AdvancedDataGrid/ViewModels/ColumnDefinitionViewModel.cs
// ===========================================
using RpaWpfComponents.AdvancedDataGrid.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RpaWpfComponents.AdvancedDataGrid.ViewModels
{
    public class ColumnDefinitionViewModel : INotifyPropertyChanged
    {
        private ColumnDefinitionModel _model;

        public ColumnDefinitionViewModel(ColumnDefinitionModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _model.PropertyChanged += Model_PropertyChanged;
        }

        public ColumnDefinitionModel Model => _model;

        public string Name
        {
            get => _model.Name;
            set
            {
                if (_model.Name != value)
                {
                    _model.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public Type DataType
        {
            get => _model.DataType;
            set
            {
                if (_model.DataType != value)
                {
                    _model.DataType = value;
                    OnPropertyChanged();
                }
            }
        }

        public double MinWidth
        {
            get => _model.MinWidth;
            set
            {
                if (_model.MinWidth != value)
                {
                    _model.MinWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public double MaxWidth
        {
            get => _model.MaxWidth;
            set
            {
                if (_model.MaxWidth != value)
                {
                    _model.MaxWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AllowResize
        {
            get => _model.AllowResize;
            set
            {
                if (_model.AllowResize != value)
                {
                    _model.AllowResize = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AllowSort
        {
            get => _model.AllowSort;
            set
            {
                if (_model.AllowSort != value)
                {
                    _model.AllowSort = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsReadOnly
        {
            get => _model.IsReadOnly;
            set
            {
                if (_model.IsReadOnly != value)
                {
                    _model.IsReadOnly = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSpecialColumn => _model.IsSpecialColumn;

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}