// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Services/Interfaces/IClipboardService.cs
// ===========================================
using System.Threading.Tasks;

namespace RpaWpfComponents.AdvancedDataGrid.Services.Interfaces
{
    public interface IClipboardService
    {
        Task<string> GetClipboardDataAsync();
        Task SetClipboardDataAsync(string data);
        Task<bool> HasClipboardDataAsync();

        string ConvertToExcelFormat(string[,] data);
        string[,] ParseFromExcelFormat(string clipboardData);
    }
}