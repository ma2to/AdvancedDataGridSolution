// ============================================
// RpaWpfComponents/SmartListBox/Services/Implementation/SelectionService.cs
// ============================================
using System;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.SmartListBox.Models;
using RpaWpfComponents.SmartListBox.Services.Interfaces;
using RpaWpfComponents.SmartListBox.Events;

// Alias pre konzistenciu
using SmartSelectionChangedEventArgs = RpaWpfComponents.SmartListBox.Events.SelectionChangedEventArgs;

namespace RpaWpfComponents.SmartListBox.Services.Implementation
{
    internal class SelectionService : ISelectionService, IDisposable
    {
        private readonly ILogger<SelectionService> _logger;
        private SelectionMode _currentMode = SelectionMode.Single;

        public SelectionService(ILogger<SelectionService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SelectionService>.Instance;
        }

        public event EventHandler<SmartSelectionChangedEventArgs>? SelectionChanged;

        public void SetSelectionMode(SelectionMode mode)
        {
            ThrowIfDisposed();
            if (_currentMode != mode)
            {
                _currentMode = mode;
                _logger.LogDebug("Selection mode changed to: {Mode}", mode);
            }
        }

        public SelectionMode GetSelectionMode()
        {
            ThrowIfDisposed();
            return _currentMode;
        }

        protected virtual void OnSelectionChanged(SmartSelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogDebug("Disposing SelectionService");
                // Clear event handlers
                SelectionChanged = null;
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SelectionService));
            }
        }

        #endregion
    }
}