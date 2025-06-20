// ===========================================
// Configuration/ServiceCollectionExtensions.cs
// ===========================================
using Microsoft.Extensions.DependencyInjection;
using Components.AdvancedDataGrid.Services.Interfaces;
using Components.AdvancedDataGrid.Services.Implementation;
using Components.AdvancedDataGrid.ViewModels;

namespace Components.AdvancedDataGrid.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAdvancedDataGrid(this IServiceCollection services)
        {
            // Register services
            services.AddScoped<IDataService, DataService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IClipboardService, ClipboardService>();
            services.AddScoped<IColumnService, ColumnService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<INavigationService, NavigationService>();

            // Register ViewModels
            services.AddTransient<AdvancedDataGridViewModel>();
            services.AddTransient<MirrorEditorViewModel>();

            return services;
        }
    }
}