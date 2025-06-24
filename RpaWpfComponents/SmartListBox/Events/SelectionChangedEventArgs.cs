// ============================================
// RpaWpfComponents/SmartListBox/Events/SelectionChangedEventArgs.cs
// ============================================
using System;
using System.Collections.Generic;
using RpaWpfComponents.SmartListBox.Models;

namespace RpaWpfComponents.SmartListBox.Events
{
    public class SelectionChangedEventArgs : EventArgs
    {
        public IEnumerable<object> SelectedItems { get; set; } = new List<object>();
        public IEnumerable<object> AddedItems { get; set; } = new List<object>();
        public IEnumerable<object> RemovedItems { get; set; } = new List<object>();
        public SelectionMode SelectionMode { get; set; }
    }
}