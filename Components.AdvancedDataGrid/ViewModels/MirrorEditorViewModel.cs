// ViewModels/MirrorEditorViewModel.cs - OPRAVENÝ
using Components.AdvancedDataGrid.Commands;
using Components.AdvancedDataGrid.Models;
using Components.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Components.AdvancedDataGrid.ViewModels
{
    public class MirrorEditorViewModel : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private DataGridCellModel? _currentCell;
        private string _currentValue = "";
        private string _originalValue = "";
        private bool _isEditing = false;
        private bool _isEditable = false;

        public MirrorEditorViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            InitializeCommands();
        }

        public DataGridCellModel? CurrentCell
        {
            get => _currentCell;
            private set => SetProperty(ref _currentCell, value);
        }

        public string CurrentValue
        {
            get => _currentValue;
            set
            {
                if (SetProperty(ref _currentValue, value))
                {
                    // Synchronizuj s aktuálnou bunkou iba ak editujeme
                    if (CurrentCell != null && IsEditing)
                    {
                        CurrentCell.Value = value;
                    }
                    OnPropertyChanged(nameof(HasChanges));
                }
            }
        }

        public string OriginalValue
        {
            get => _originalValue;
            private set => SetProperty(ref _originalValue, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    UpdateCanExecuteCommands();
                    OnPropertyChanged(nameof(HasChanges));
                }
            }
        }

        public bool IsEditable
        {
            get => _isEditable;
            private set => SetProperty(ref _isEditable, value);
        }

        public bool HasChanges => IsEditing && CurrentValue != OriginalValue;

        public string EditorInfo
        {
            get
            {
                if (CurrentCell == null)
                    return "Žiadna bunka nie je aktívna";

                var charCount = CurrentValue?.Length ?? 0;
                var lineCount = CurrentValue?.Split('\n').Length ?? 1;
                var wordCount = string.IsNullOrWhiteSpace(CurrentValue) ? 0 :
                    CurrentValue.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                return $"{CurrentCell.ColumnName} | Znakov: {charCount} | Slov: {wordCount} | Riadkov: {lineCount}";
            }
        }

        public ICommand CommitChangesCommand { get; private set; } = null!;
        public ICommand CancelChangesCommand { get; private set; } = null!;

        public void SetCurrentCell(DataGridCellModel? cell)
        {
            try
            {
                // Ak editujeme predchádzajúcu bunku, uložme zmeny
                if (IsEditing && CurrentCell != null && HasChanges)
                {
                    CommitChanges();
                }

                CurrentCell = cell;

                if (cell != null)
                {
                    OriginalValue = cell.Value?.ToString() ?? "";
                    CurrentValue = OriginalValue;
                    IsEditable = true;
                }
                else
                {
                    OriginalValue = "";
                    CurrentValue = "";
                    IsEditable = false;
                }

                IsEditing = false;
                OnPropertyChanged(nameof(EditorInfo));
                OnPropertyChanged(nameof(HasChanges));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MirrorEditor SetCurrentCell error: {ex.Message}");
            }
        }

        public void StartEditing()
        {
            try
            {
                if (CurrentCell != null && IsEditable)
                {
                    IsEditing = true;
                    OriginalValue = CurrentCell.Value?.ToString() ?? "";
                    CurrentValue = OriginalValue;
                    OnPropertyChanged(nameof(HasChanges));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MirrorEditor StartEditing error: {ex.Message}");
            }
        }

        public void CommitChanges()
        {
            try
            {
                if (CurrentCell != null && IsEditing)
                {
                    // Uložme hodnotu do bunky
                    CurrentCell.Value = CurrentValue;
                    OriginalValue = CurrentValue;
                    IsEditing = false;
                    OnPropertyChanged(nameof(HasChanges));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MirrorEditor CommitChanges error: {ex.Message}");
            }
        }

        public void CancelChanges()
        {
            try
            {
                if (CurrentCell != null)
                {
                    // Vrátime hodnotu na pôvodnú
                    CurrentValue = OriginalValue;

                    if (IsEditing && CurrentCell != null)
                    {
                        // Vrátime aj hodnotu v bunke
                        CurrentCell.Value = OriginalValue;
                    }

                    IsEditing = false;
                    OnPropertyChanged(nameof(HasChanges));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MirrorEditor CancelChanges error: {ex.Message}");
            }
        }

        private void InitializeCommands()
        {
            CommitChangesCommand = new RelayCommand(
                () => CommitChanges(),
                () => IsEditing && HasChanges);

            CancelChangesCommand = new RelayCommand(
                () => CancelChanges(),
                () => IsEditing);
        }

        private void UpdateCanExecuteCommands()
        {
            if (CommitChangesCommand is RelayCommand commitCmd)
                commitCmd.RaiseCanExecuteChanged();

            if (CancelChangesCommand is RelayCommand cancelCmd)
                cancelCmd.RaiseCanExecuteChanged();
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

            // Update commands when properties change
            if (propertyName == nameof(IsEditing) || propertyName == nameof(HasChanges))
            {
                UpdateCanExecuteCommands();
            }

            // Update EditorInfo when CurrentValue changes
            if (propertyName == nameof(CurrentValue))
            {
                OnPropertyChanged(nameof(EditorInfo));
            }
        }
    }
}