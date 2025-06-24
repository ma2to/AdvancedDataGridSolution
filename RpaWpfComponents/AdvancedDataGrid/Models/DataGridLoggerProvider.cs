// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Services/DataGridLoggerProvider.cs - NOVÝ SÚBOR
// ===========================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RpaWpfComponents.AdvancedDataGrid.Interfaces;

namespace RpaWpfComponents.AdvancedDataGrid.Services
{
    /// <summary>
    /// Implementácia IDataGridLoggerProvider pre produkčné použitie
    /// </summary>
    public class DataGridLoggerProvider : IDataGridLoggerProvider
    {
        private readonly ILoggerFactory? _loggerFactory;

        public DataGridLoggerProvider(ILoggerFactory? loggerFactory = null)
        {
            _loggerFactory = loggerFactory;
        }

        public ILogger<T> CreateLogger<T>()
        {
            return _loggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory?.CreateLogger(categoryName) ?? NullLogger.Instance;
        }
    }

    /// <summary>
    /// Null Object Pattern implementácia pre prípady kde logging nie je potrebný
    /// </summary>
    public class NullDataGridLoggerProvider : IDataGridLoggerProvider
    {
        public static readonly NullDataGridLoggerProvider Instance = new();

        private NullDataGridLoggerProvider() { }

        public ILogger<T> CreateLogger<T>()
        {
            return NullLogger<T>.Instance;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }
    }
}