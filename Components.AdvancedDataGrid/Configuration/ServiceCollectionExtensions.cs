// RpaWpfComponents/AdvancedDataGrid/Configuration/ServiceCollectionExtensions.cs
using RpaWpfComponents.AdvancedDataGrid.Services.Implementation;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using RpaWpfComponents.AdvancedDataGrid.ViewModels;
using Microsoft.Extensions.DependencyInjection;


namespace RpaWpfComponents.AdvancedDataGrid.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAdvancedDataGrid(this IServiceCollection services)
        {
            services.AddScoped<IDataService, DataService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IClipboardService, ClipboardService>();
            services.AddScoped<IColumnService, ColumnService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<INavigationService, NavigationService>();

            services.AddTransient<AdvancedDataGridViewModel>();

            return services;
        }
    }
}