// ============================================
// RpaWpfComponents/SmartListBox/Services/Interfaces/ISelectionService.cs
// ============================================
using System;
using RpaWpfComponents.SmartListBox.Models;
using RpaWpfComponents.SmartListBox.Events;

// Alias pre konzistenciu
using SmartSelectionChangedEventArgs = RpaWpfComponents.SmartListBox.Events.SelectionChangedEventArgs;

namespace RpaWpfComponents.SmartListBox.Services.Interfaces
{
    public interface ISelectionService
    {
        void SetSelectionMode(SelectionMode mode);
        SelectionMode GetSelectionMode();
        event EventHandler<SmartSelectionChangedEventArgs>? SelectionChanged;
    }
}