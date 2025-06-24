// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Helpers/ErrorHelper.cs
// ===========================================
using System;
using System.Text;
using RpaWpfComponents.AdvancedDataGrid.Events;

namespace RpaWpfComponents.AdvancedDataGrid.Helpers
{
    internal static class ErrorHelper
    {
        public static string FormatErrorMessage(ComponentErrorEventArgs errorArgs)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Operation: {errorArgs.Operation}");
            sb.AppendLine($"Time: {errorArgs.Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Error: {errorArgs.Exception.Message}");

            if (!string.IsNullOrEmpty(errorArgs.AdditionalInfo))
            {
                sb.AppendLine($"Additional Info: {errorArgs.AdditionalInfo}");
            }

            if (errorArgs.Exception.InnerException != null)
            {
                sb.AppendLine($"Inner Exception: {errorArgs.Exception.InnerException.Message}");
            }

            return sb.ToString();
        }

        public static void LogError(ComponentErrorEventArgs errorArgs, Action<string> logger = null)
        {
            var message = FormatErrorMessage(errorArgs);

            if (logger != null)
            {
                logger(message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
            }
        }
    }
}