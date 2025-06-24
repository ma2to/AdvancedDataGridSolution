// ============================================
// RpaWpfComponents/SmartListBox/Configuration/ServiceCollectionExtensions.cs
// ============================================
using Microsoft.Extensions.DependencyInjection;
using RpaWpfComponents.SmartListBox.Services.Implementation;
using RpaWpfComponents.SmartListBox.Services.Interfaces;
using RpaWpfComponents.SmartListBox.ViewModels;

namespace RpaWpfComponents.SmartListBox.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSmartListBox(this IServiceCollection services)
        {
            services.AddScoped<IDataProcessingService, DataProcessingService>();
            services.AddScoped<ISelectionService, SelectionService>();
            services.AddTransient<SmartListBoxViewModel>();

            return services;
        }
    }
}