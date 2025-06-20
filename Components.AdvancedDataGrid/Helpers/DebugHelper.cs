// Helpers/DebugHelper.cs - NOVÝ
using System;
using System.Diagnostics;
using Components.AdvancedDataGrid.Events;

namespace Components.AdvancedDataGrid.Helpers
{
    public static class DebugHelper
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

            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {message}");
        }

        public static void LogError(Exception ex, string operation, string category = "Error")
        {
            if (!_isDebugEnabled) return;

            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {operation}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{category}] Inner: {ex.InnerException.Message}");
            }
        }

        public static void LogValidation(string columnName, string value, bool isValid, string errors = "")
        {
            if (!_isDebugEnabled) return;

            var status = isValid ? "✓ VALID" : "✗ INVALID";
            var errorInfo = isValid ? "" : $" | Errors: {errors}";
            Log($"{status} | {columnName} = '{value}'{errorInfo}", "Validation");
        }

        public static void LogNavigation(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!_isDebugEnabled) return;

            Log($"Navigation: [{fromRow},{fromCol}] → [{toRow},{toCol}]", "Navigation");
        }

        public static void LogDataOperation(string operation, int rowCount, int columnCount = 0)
        {
            if (!_isDebugEnabled) return;

            var info = columnCount > 0 ? $"{rowCount} rows, {columnCount} columns" : $"{rowCount} rows";
            Log($"{operation}: {info}", "Data");
        }

        public static void LogMirrorEditor(string operation, string currentValue, bool isEditing)
        {
            if (!_isDebugEnabled) return;

            var mode = isEditing ? "EDITING" : "VIEWING";
            var valuePreview = currentValue?.Length > 20 ? currentValue.Substring(0, 20) + "..." : currentValue;
            Log($"{operation} | {mode} | Value: '{valuePreview}'", "MirrorEditor");
        }

        public static void LogClipboard(string operation, int rows = 0, int cols = 0)
        {
            if (!_isDebugEnabled) return;

            var size = rows > 0 ? $"{rows}×{cols}" : "unknown size";
            Log($"{operation}: {size}", "Clipboard");
        }

        public static void LogComponent(string component, string message)
        {
            if (!_isDebugEnabled) return;

            Log(message, component);
        }

        public static void EnableDebug()
        {
            _isDebugEnabled = true;
            Log("Debug logging enabled", "Debug");
        }

        public static void DisableDebug()
        {
            Log("Debug logging disabled", "Debug");
            _isDebugEnabled = false;
        }
    }
}