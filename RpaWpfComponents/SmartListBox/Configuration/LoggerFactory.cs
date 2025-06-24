// ============================================
// RpaWpfComponents/SmartListBox/Configuration/LoggerFactory.cs
// ============================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RpaWpfComponents.SmartListBox.Configuration
{
    internal static class LoggerFactory
    {
        private static ILoggerFactory? _loggerFactory;

        public static void Configure(ILoggerFactory? loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public static ILogger<T> CreateLogger<T>()
        {
            return _loggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;
        }
    }
}