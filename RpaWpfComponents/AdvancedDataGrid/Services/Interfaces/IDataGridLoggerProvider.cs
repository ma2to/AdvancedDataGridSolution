// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Interfaces/IDataGridLoggerProvider.cs - NOVÝ SÚBOR
// ===========================================
using Microsoft.Extensions.Logging;

namespace RpaWpfComponents.AdvancedDataGrid.Interfaces
{
    /// <summary>
    /// Interface pre poskytovanie loggerov v AdvancedDataGrid komponente
    /// Umožňuje lepšiu testovateľnosť ako static LoggerFactory
    /// </summary>
    public interface IDataGridLoggerProvider
    {
        /// <summary>
        /// Vytvorí logger pre špecifický typ
        /// </summary>
        ILogger<T> CreateLogger<T>();

        /// <summary>
        /// Vytvorí logger s názvom kategórie
        /// </summary>
        ILogger CreateLogger(string categoryName);
    }
}