// ============================================
// RpaWpfComponents/SmartListBox/SmartListBoxControl.cs - HLAVNÝ WRAPPER
// ============================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.SmartListBox.Views;
using RpaWpfComponents.SmartListBox.Models;
using RpaWpfComponents.SmartListBox.Events;

// Aliasy pre odstránenie ambiguity
using SmartSelectionChangedEventArgs = RpaWpfComponents.SmartListBox.Events.SelectionChangedEventArgs;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace RpaWpfComponents.SmartListBox
{
    /// <summary>
    /// Inteligentný ListBox komponent s konfigurovateľnou selekciou
    /// </summary>
    public class SmartListBoxControl : UserControl, IAsyncDisposable
    {
        private readonly SmartListBoxView _internalView;
        private bool _disposed = false;

        public SmartListBoxControl()
        {
            _internalView = new SmartListBoxView();
            Content = _internalView;
            _internalView.ErrorOccurred += OnInternalError;
            _internalView.SelectionChanged += OnInternalSelectionChanged;
        }

        #region Events

        /// <summary>
        /// Event ktorý sa spustí pri chybe v komponente
        /// </summary>
        public event EventHandler<ComponentError>? ErrorOccurred;

        /// <summary>
        /// Event ktorý sa spustí pri zmene selekcie
        /// </summary>
        public event EventHandler<SelectionChangedArgs>? SelectionChanged;

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
                    // Unsubscribe od eventov
                    if (_internalView != null)
                    {
                        _internalView.ErrorOccurred -= OnInternalError;
                        _internalView.SelectionChanged -= OnInternalSelectionChanged;
                    }

                    // Dispose internal view
                    if (_internalView is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (_internalView is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    _disposed = true;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(new ComponentError(ex, "DisposeAsync"));
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
                throw new ObjectDisposedException(nameof(SmartListBoxControl));
            }
        }

        #endregion

        #region Inicializácia

        /// <summary>
        /// Inicializuje komponent s dátami a nastaveniami
        /// </summary>
        /// <param name="data">Dáta na zobrazenie</param>
        /// <param name="loggerFactory">Logger factory pre logovanie</param>
        /// <param name="selectionMode">Režim selekcie - jeden alebo viac itemov</param>
        public async Task Initialize(IEnumerable<object> data, ILoggerFactory? loggerFactory = null, SelectionMode selectionMode = SelectionMode.Single)
        {
            try
            {
                await _internalView.InitializeAsync(data, loggerFactory, selectionMode);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "Initialize"));
            }
        }

        #endregion

        #region Správa dát

        /// <summary>
        /// Aktualizuje dáta v komponente
        /// </summary>
        /// <param name="newData">Nové dáta na zobrazenie</param>
        public async Task UpdateData(IEnumerable<object> newData)
        {
            try
            {
                await _internalView.UpdateDataAsync(newData);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "UpdateData"));
            }
        }

        /// <summary>
        /// Zruší všetky selektnuté položky
        /// </summary>
        public void ClearSelection()
        {
            ThrowIfDisposed();
            try
            {
                _internalView.ClearSelection();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "ClearSelection"));
            }
        }

        /// <summary>
        /// Vráti všetky selektnuté položky (originálne hodnoty, nie display text)
        /// Ak je zobrazený len názov súboru, vráti sa celá cesta
        /// </summary>
        /// <returns>Zoznam originálnych hodnôt selektnutých položiek</returns>
        public IEnumerable<object> GetSelectedItems()
        {
            try
            {
                return _internalView.GetSelectedItems();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "GetSelectedItems"));
                return Enumerable.Empty<object>();
            }
        }

        /// <summary>
        /// Vráti prvú selektnútú položku alebo null (originálna hodnota, nie display text)
        /// </summary>
        /// <returns>Prvá originálna hodnota selektnutej položky alebo null</returns>
        public object? GetSelectedItem()
        {
            try
            {
                return _internalView.GetSelectedItems().FirstOrDefault();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentError(ex, "GetSelectedItem"));
                return null;
            }
        }

        #endregion

        #region Vlastnosti

        /// <summary>
        /// Aktuálny režim selekcie
        /// </summary>
        public SelectionMode SelectionMode
        {
            get => _internalView.SelectionMode;
            set => _internalView.SelectionMode = value;
        }

        /// <summary>
        /// Počet selektnutých položiek
        /// </summary>
        public int SelectedCount => _internalView.GetSelectedItems().Count();

        /// <summary>
        /// Celkový počet položiek
        /// </summary>
        public int TotalCount => _internalView.TotalCount;

        #endregion

        #region Private Event Handlers

        private void OnInternalError(object? sender, ComponentErrorEventArgs e)
        {
            OnErrorOccurred(new ComponentError(e.Exception, e.Operation, e.AdditionalInfo));
        }

        private void OnInternalSelectionChanged(object? sender, SmartSelectionChangedEventArgs e)
        {
            var args = new SelectionChangedArgs
            {
                SelectedItems = e.SelectedItems,
                AddedItems = e.AddedItems,
                RemovedItems = e.RemovedItems,
                SelectionMode = e.SelectionMode
            };
            OnSelectionChanged(args);
        }

        private void OnErrorOccurred(ComponentError error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        private void OnSelectionChanged(SelectionChangedArgs args)
        {
            SelectionChanged?.Invoke(this, args);
        }

        #endregion
    }

    #region Public API Models

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

    /// <summary>
    /// Argumenty pre SelectionChanged event
    /// </summary>
    public class SelectionChangedArgs : EventArgs
    {
        /// <summary>
        /// Všetky selektnuté položky (originálne hodnoty)
        /// </summary>
        public IEnumerable<object> SelectedItems { get; set; } = new List<object>();

        /// <summary>
        /// Novo pridané položky (originálne hodnoty)
        /// </summary>
        public IEnumerable<object> AddedItems { get; set; } = new List<object>();

        /// <summary>
        /// Odstránené položky (originálne hodnoty)
        /// </summary>
        public IEnumerable<object> RemovedItems { get; set; } = new List<object>();

        /// <summary>
        /// Aktuálny režim selekcie
        /// </summary>
        public SelectionMode SelectionMode { get; set; }
    }

    /// <summary>
    /// Informácie o chybe v komponente
    /// </summary>
    public class ComponentError : EventArgs
    {
        public Exception Exception { get; set; }
        public string Operation { get; set; }
        public string AdditionalInfo { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ComponentError(Exception exception, string operation, string additionalInfo = "")
        {
            Exception = exception;
            Operation = operation;
            AdditionalInfo = additionalInfo ?? "";
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Operation}: {Exception.Message}" +
                   (string.IsNullOrEmpty(AdditionalInfo) ? "" : $" - {AdditionalInfo}");
        }
    }

    #endregion
}