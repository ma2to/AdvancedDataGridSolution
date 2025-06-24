// ============================================
// RpaWpfComponents/SmartListBox/ServiceCollectionExtensions.cs - Top-level bez konfliktov
// ============================================
using Microsoft.Extensions.DependencyInjection;
using RpaWpfComponents.SmartListBox.Services.Implementation;
using RpaWpfComponents.SmartListBox.Services.Interfaces;
using RpaWpfComponents.SmartListBox.ViewModels;

namespace RpaWpfComponents.SmartListBox
{
    /// <summary>
    /// Extension metódy pre IServiceCollection na registráciu SmartListBox služieb
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Pridá SmartListBox služby do DI kontajnera
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Upravenú service collection pre method chaining</returns>
        public static IServiceCollection AddSmartListBox(this IServiceCollection services)
        {
            // Registruj služby
            services.AddScoped<IDataProcessingService, DataProcessingService>();
            services.AddScoped<ISelectionService, SelectionService>();

            // Registruj ViewModel ako transient (nová inštancia pre každé použitie)
            services.AddTransient<SmartListBoxViewModel>();

            return services;
        }
    }
}