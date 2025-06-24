// ============================================
// RpaWpfComponents/SmartListBox/Services/Implementation/DataProcessingService.cs
// ============================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.SmartListBox.Models;
using RpaWpfComponents.SmartListBox.Services.Interfaces;

namespace RpaWpfComponents.SmartListBox.Services.Implementation
{
    internal class DataProcessingService : IDataProcessingService, IDisposable
    {
        private readonly ILogger<DataProcessingService> _logger;

        public DataProcessingService(ILogger<DataProcessingService>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DataProcessingService>.Instance;
        }

        public async Task<IEnumerable<SmartListBoxItem>> ProcessDataAsync(IEnumerable<object>? data)
        {
            ThrowIfDisposed();
            return await Task.Run(() =>
            {
                try
                {
                    if (data == null)
                    {
                        _logger.LogWarning("ProcessDataAsync called with null data");
                        return Enumerable.Empty<SmartListBoxItem>();
                    }

                    var items = data.Select(CreateItem).ToList();

                    _logger.LogInformation("Processed {ItemCount} items", items.Count);
                    return items;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing data");
                    return Enumerable.Empty<SmartListBoxItem>();
                }
            });
        }

        public SmartListBoxItem CreateItem(object? value)
        {
            ThrowIfDisposed();
            try
            {
                var item = new SmartListBoxItem(value);
                _logger.LogTrace("Created item: {DisplayText} ({DataType})", item.DisplayText, item.DataType);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item for value: {Value}", value);
                return new SmartListBoxItem(value);
            }
        }

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogDebug("Disposing DataProcessingService");
                // No specific resources to clean up in this service
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DataProcessingService));
            }
        }

        #endregion
    }
}