// ============================================
// RpaWpfComponents/SmartListBox/Helpers/DebugHelper.cs
// ============================================
using System;
using Microsoft.Extensions.Logging;

namespace RpaWpfComponents.SmartListBox.Helpers
{
    internal static class DebugHelper
    {
        private static bool _isDebugEnabled = true;

        public static bool IsDebugEnabled
        {
            get => _isDebugEnabled;
            set => _isDebugEnabled = value;
        }

        public static void Log(string message, string category = "General")
        {
            if (!_isDebugEnabled) return;

            var formattedMessage = $"[{category}] {message}";
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {formattedMessage}");
        }

        public static void LogError(Exception ex, string operation, string category = "Error")
        {
            if (!_isDebugEnabled) return;

            var errorMessage = $"[{category}] {operation}: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {errorMessage}");
        }
    }
}