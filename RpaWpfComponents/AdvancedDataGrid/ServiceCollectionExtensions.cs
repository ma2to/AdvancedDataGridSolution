using Microsoft.Extensions.DependencyInjection;

namespace RpaWpfComponents.AdvancedDataGrid
{
    /// <summary>
    /// Extension metódy pre IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Pridá AdvancedDataGrid služby do DI kontajnera
        /// </summary>
        public static IServiceCollection AddAdvancedDataGrid(this IServiceCollection services)
        {
            return RpaWpfComponents.AdvancedDataGrid.Configuration.ServiceCollectionExtensions.AddAdvancedDataGrid(services);
        }
    }
}