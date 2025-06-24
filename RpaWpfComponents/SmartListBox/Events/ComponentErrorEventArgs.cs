// ============================================
// RpaWpfComponents/SmartListBox/Events/ComponentErrorEventArgs.cs
// ============================================
using System;

namespace RpaWpfComponents.SmartListBox.Events
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
    }
}