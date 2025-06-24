// ============================================
// RpaWpfComponents/SmartListBox/Services/Interfaces/IDataProcessingService.cs
// ============================================
using System.Collections.Generic;
using System.Threading.Tasks;
using RpaWpfComponents.SmartListBox.Models;

namespace RpaWpfComponents.SmartListBox.Services.Interfaces
{
    public interface IDataProcessingService
    {
        Task<IEnumerable<SmartListBoxItem>> ProcessDataAsync(IEnumerable<object>? data);
        SmartListBoxItem CreateItem(object? value);
    }
}