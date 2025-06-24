// ============================================
// RpaWpfComponents/SmartListBox/ViewModels/SmartListBoxViewModel.cs
// ============================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.SmartListBox.Models;
using RpaWpfComponents.SmartListBox.Services.Interfaces;
using RpaWpfComponents.SmartListBox.Commands;
using RpaWpfComponents.SmartListBox.Events;

// Alias pre konzistenciu
using SmartSelectionChangedEventArgs = RpaWpfComponents.SmartListBox.Events.SelectionChangedEventArgs;

namespace RpaWpfComponents.SmartListBox.ViewModels
{
    /// <summary>
    /// ViewModel pre SmartListBox komponent
    /// </summary>
    public class SmartListBoxViewModel : INotifyPropertyChanged, IAsyncDisposable
    {
        private readonly IDataProcessingService _dataProcessingService;
        private readonly ISelectionService _selectionService;
        private readonly ILogger<SmartListBoxViewModel> _logger;

        private ObservableCollection<SmartListBoxItem> _items = new();
        private SelectionMode _selectionMode = SelectionMode.Single;
        private bool _isInitialized = false;
        private bool _disposed = false;

        public SmartListBoxViewModel(
            IDataProcessingService dataProcessingService,
            ISelectionService selectionService,
            ILogger<SmartListBoxViewModel>? logger = null)
        {
            _dataProcessingService = dataProcessingService ?? throw new ArgumentNullException(nameof(dataProcessingService));
            _selectionService = selectionService ?? throw new ArgumentNullException(nameof(selectionService));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SmartListBoxViewModel>.Instance;

            InitializeCommands();
            SubscribeToEvents();
        }

        #region Properties

        /// <summary>
        /// Kolekcia položiek v ListBox
        /// </summary>
        public ObservableCollection<SmartListBoxItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// Režim selekcie
        /// </summary>
        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                if (SetProperty(ref _selectionMode, value))
                {
                    _selectionService.SetSelectionMode(value);
                    if (value == SelectionMode.Single)
                    {
                        ClearSelection();
                    }
                }
            }
        }

        /// <summary>
        /// Či je komponent inicializovaný
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Commands

        public ICommand ItemClickCommand { get; private set; } = null!;
        public ICommand ClearSelectionCommand { get; private set; } = null!;

        #endregion

        #region Events

        public event EventHandler<SmartSelectionChangedEventArgs>? SelectionChanged;
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        #endregion

        #region Public Methods

        /// <summary>
        /// Inicializuje ViewModel s dátami
        /// </summary>
        public async Task InitializeAsync(IEnumerable<object> data, SelectionMode selectionMode = SelectionMode.Single)
        {
            ThrowIfDisposed();
            try
            {
                _logger.LogInformation("Initializing SmartListBox with {ItemCount} items", data?.Count() ?? 0);

                SelectionMode = selectionMode;

                var processedItems = await _dataProcessingService.ProcessDataAsync(data);

                Items.Clear();
                foreach (var item in processedItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                    Items.Add(item);
                }

                _isInitialized = true;
                _logger.LogInformation("SmartListBox initialized successfully with {ItemCount} items", Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing SmartListBox");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
            }
        }

        /// <summary>
        /// Aktualizuje dáta
        /// </summary>
        public async Task UpdateDataAsync(IEnumerable<object> newData)
        {
            ThrowIfDisposed();
            try
            {
                _logger.LogInformation("Updating SmartListBox data with {ItemCount} items", newData?.Count() ?? 0);

                // Unsubscribe od starých itemov
                foreach (var item in Items)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }

                var processedItems = await _dataProcessingService.ProcessDataAsync(newData);

                Items.Clear();
                foreach (var item in processedItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                    Items.Add(item);
                }

                _logger.LogInformation("SmartListBox data updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SmartListBox data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "UpdateDataAsync"));
            }
        }

        /// <summary>
        /// Zruší všetky selekcie
        /// </summary>
        public void ClearSelection()
        {
            try
            {
                var selectedItems = Items.Where(i => i.IsSelected).ToList();

                foreach (var item in selectedItems)
                {
                    item.IsSelected = false;
                }

                _logger.LogDebug("Selection cleared, {Count} items unselected", selectedItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing selection");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearSelection"));
            }
        }

        /// <summary>
        /// Vráti selektnuté položky
        /// </summary>
        public IEnumerable<object> GetSelectedItems()
        {
            ThrowIfDisposed();
            return Items.Where(i => i.IsSelected).Select(i => i.OriginalValue).Where(v => v != null)!;
        }

        #endregion

        #region Private Methods

        private void InitializeCommands()
        {
            ItemClickCommand = new RelayCommand<SmartListBoxItem>(OnItemClick);
            ClearSelectionCommand = new RelayCommand(ClearSelection);
        }

        private void SubscribeToEvents()
        {
            _selectionService.SelectionChanged += OnSelectionServiceChanged;
        }

        private void OnItemClick(SmartListBoxItem? item)
        {
            if (item == null) return;

            try
            {
                var previouslySelected = Items.Where(i => i.IsSelected).ToList();

                if (SelectionMode == SelectionMode.Single)
                {
                    // Single mode: unselect všetko ostatné
                    foreach (var otherItem in Items.Where(i => i != item))
                    {
                        otherItem.IsSelected = false;
                    }

                    // Toggle selected item
                    item.IsSelected = !item.IsSelected;
                }
                else
                {
                    // Multiple mode: len toggle clicked item
                    item.IsSelected = !item.IsSelected;
                }

                var currentlySelected = Items.Where(i => i.IsSelected).ToList();
                var addedItems = currentlySelected.Except(previouslySelected);
                var removedItems = previouslySelected.Except(currentlySelected);

                OnSelectionChanged(new SmartSelectionChangedEventArgs
                {
                    SelectedItems = currentlySelected.Select(i => i.OriginalValue).Where(v => v != null)!,
                    AddedItems = addedItems.Select(i => i.OriginalValue).Where(v => v != null)!,
                    RemovedItems = removedItems.Select(i => i.OriginalValue).Where(v => v != null)!,
                    SelectionMode = SelectionMode
                });

                _logger.LogDebug("Item selection changed: {ItemDisplay}, Selected: {IsSelected}",
                    item.DisplayText, item.IsSelected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling item click");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnItemClick"));
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SmartListBoxItem.IsSelected) && sender is SmartListBoxItem item)
            {
                // Handle selection changes if needed
            }
        }

        private void OnSelectionServiceChanged(object? sender, SmartSelectionChangedEventArgs e)
        {
            OnSelectionChanged(e);
        }

        #endregion

        #region Event Handlers

        protected virtual void OnSelectionChanged(SmartSelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        #endregion

        #region IAsyncDisposable

        /// <summary>
        /// Asynchronne uvoľní všetky resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                try
                {
                    _logger.LogDebug("Disposing SmartListBoxViewModel...");

                    // Unsubscribe od eventov
                    if (_selectionService != null)
                    {
                        _selectionService.SelectionChanged -= OnSelectionServiceChanged;
                    }

                    // Unsubscribe od itemov
                    if (_items != null)
                    {
                        foreach (var item in _items)
                        {
                            if (item != null)
                            {
                                item.PropertyChanged -= Item_PropertyChanged;
                            }
                        }
                    }

                    // Dispose services ak implementujú IAsyncDisposable
                    if (_dataProcessingService is IAsyncDisposable dataServiceAsync)
                    {
                        await dataServiceAsync.DisposeAsync();
                    }
                    else if (_dataProcessingService is IDisposable dataServiceSync)
                    {
                        dataServiceSync.Dispose();
                    }

                    if (_selectionService is IAsyncDisposable selectionServiceAsync)
                    {
                        await selectionServiceAsync.DisposeAsync();
                    }
                    else if (_selectionService is IDisposable selectionServiceSync)
                    {
                        selectionServiceSync.Dispose();
                    }

                    // Clear collections
                    _items?.Clear();

                    _disposed = true;
                    _logger.LogDebug("SmartListBoxViewModel disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing SmartListBoxViewModel");
                    OnErrorOccurred(new ComponentErrorEventArgs(ex, "DisposeAsync"));
                }
            }
        }

        /// <summary>
        /// Kontrola či je objekt disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SmartListBoxViewModel));
            }
        }

        #endregion

        #region INotifyPropertyChanged

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

        #endregion
    }
}