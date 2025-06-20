// ===========================================
// Events/ComponentErrorEventArgs.cs
// ===========================================
using System;

namespace Components.AdvancedDataGrid.Events
{
    public class ComponentErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public string Operation { get; set; }
        public string AdditionalInfo { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ComponentErrorEventArgs(Exception exception, string operation, string additionalInfo = null)
        {
            Exception = exception;
            Operation = operation;
            AdditionalInfo = additionalInfo;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Operation}: {Exception.Message}" +
                   (string.IsNullOrEmpty(AdditionalInfo) ? "" : $" - {AdditionalInfo}");
        }
    }
}