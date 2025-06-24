// ===========================================
// RpaWpfComponents/AdvancedDataGrid/ViewModels/DataRowViewModel.cs
// ===========================================
using RpaWpfComponents.AdvancedDataGrid.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RpaWpfComponents.AdvancedDataGrid.ViewModels
{
    public class DataRowViewModel : INotifyPropertyChanged
    {
        private DataGridRowModel _model;

        public DataRowViewModel(DataGridRowModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _model.PropertyChanged += Model_PropertyChanged;
        }

        public DataGridRowModel Model => _model;

        public bool HasValidationErrors => _model.HasValidationErrors;
        public bool IsEmpty => _model.IsEmpty;
        public string ValidationErrorsText => _model.ValidationErrorsText;

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