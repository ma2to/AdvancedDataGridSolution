// ============================================
// RpaWpfComponents/SmartListBox/Views/SmartListBoxView.xaml.cs
// ============================================
using Microsoft.Extensions.Logging;
using RpaWpfComponents.SmartListBox.Configuration;
using RpaWpfComponents.SmartListBox.Events;
using RpaWpfComponents.SmartListBox.Models;
using RpaWpfComponents.SmartListBox.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

// Aliasy pre odstránenie ambiguity
using SmartSelectionChangedEventArgs = RpaWpfComponents.SmartListBox.Events.SelectionChangedEventArgs;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace RpaWpfComponents.SmartListBox.Views
{
    internal partial class SmartListBoxView : UserControl, IAsyncDisposable
    {
        private SmartListBoxViewModel? _viewModel;
        private bool _disposed = false;

        public SmartListBoxView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        #region Events

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        public event EventHandler<SmartSelectionChangedEventArgs>? SelectionChanged;

        #endregion

        #region Properties

        public SelectionMode SelectionMode
        {
            get => _viewModel?.SelectionMode ?? SelectionMode.Single;
            set
            {
                if (_viewModel != null)
                {
                    _viewModel.SelectionMode = value;
                }
            }
        }

        public int TotalCount => _viewModel?.Items.Count ?? 0;

        #endregion

        #region Public Methods

        public async Task InitializeAsync(IEnumerable<object> data, ILoggerFactory? loggerFactory = null, SelectionMode selectionMode = SelectionMode.Single)
        {
            try
            {
                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel(loggerFactory);
                    DataContext = _viewModel;
                }

                await _viewModel.InitializeAsync(data, selectionMode);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
            }
        }

        public async Task UpdateDataAsync(IEnumerable<object> newData)
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.UpdateDataAsync(newData);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "UpdateDataAsync"));
            }
        }

        public void ClearSelection()
        {
            try
            {
                _viewModel?.ClearSelection();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearSelection"));
            }
        }

        public IEnumerable<object> GetSelectedItems()
        {
            try
            {
                return _viewModel?.GetSelectedItems() ?? Enumerable.Empty<object>();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "GetSelectedItems"));
                return Enumerable.Empty<object>();
            }
        }

        #endregion

        #region Private Methods

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_viewModel == null)
            {
                _viewModel = CreateViewModel();
                DataContext = _viewModel;
            }
        }

        private SmartListBoxViewModel CreateViewModel(ILoggerFactory? loggerFactory = null)
        {
            try
            {
                // Pokús sa získať ViewModel cez DI
                var viewModel = DependencyInjectionConfig.GetService<SmartListBoxViewModel>();

                // Ak DI nie je dostupné, vytvor ViewModel manuálne
                if (viewModel == null)
                {
                    viewModel = DependencyInjectionConfig.CreateViewModelWithoutDI(loggerFactory);
                }

                // Subscribe na ViewModel eventy
                viewModel.ErrorOccurred += OnViewModelError;
                viewModel.SelectionChanged += OnViewModelSelectionChanged;

                return viewModel;
            }
            catch
            {
                // Fallback: vytvor ViewModel bez DI
                var viewModel = DependencyInjectionConfig.CreateViewModelWithoutDI(loggerFactory);
                viewModel.ErrorOccurred += OnViewModelError;
                viewModel.SelectionChanged += OnViewModelSelectionChanged;
                return viewModel;
            }
        }

        private void OnViewModelError(object? sender, ComponentErrorEventArgs e)
        {
            OnErrorOccurred(e);
        }

        private void OnViewModelSelectionChanged(object? sender, SmartSelectionChangedEventArgs e)
        {
            OnSelectionChanged(e);
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual void OnSelectionChanged(SmartSelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        #endregion

        #region IAsyncDisposable

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (!_disposed && _viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnViewModelError;
                    _viewModel.SelectionChanged -= OnViewModelSelectionChanged;

                    if (_viewModel is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _disposed = true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnUnloaded"));
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                try
                {
                    if (_viewModel != null)
                    {
                        _viewModel.ErrorOccurred -= OnViewModelError;
                        _viewModel.SelectionChanged -= OnViewModelSelectionChanged;

                        if (_viewModel is IAsyncDisposable asyncDisposable)
                        {
                            await asyncDisposable.DisposeAsync();
                        }
                        else if (_viewModel is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }

                    _disposed = true;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(new ComponentErrorEventArgs(ex, "DisposeAsync"));
                }
            }
        }

        #endregion
    }
}