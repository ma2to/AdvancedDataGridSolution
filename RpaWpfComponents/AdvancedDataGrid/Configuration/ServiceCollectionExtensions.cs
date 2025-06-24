// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Configuration/ServiceCollectionExtensions.cs - KOMPLETNÁ NÁHRADA
// ===========================================
using RpaWpfComponents.AdvancedDataGrid.Interfaces;
using RpaWpfComponents.AdvancedDataGrid.Services;
using RpaWpfComponents.AdvancedDataGrid.Services.Implementation;
using RpaWpfComponents.AdvancedDataGrid.Services.Interfaces;
using RpaWpfComponents.AdvancedDataGrid.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RpaWpfComponents.AdvancedDataGrid.Configuration
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registruje všetky služby potrebné pre AdvancedDataGrid s vlastným logger factory
        /// </summary>
        public static IServiceCollection AddAdvancedDataGrid(this IServiceCollection services, ILoggerFactory? loggerFactory = null)
        {
            // Register logger provider
            if (loggerFactory != null)
            {
                services.AddSingleton<IDataGridLoggerProvider>(new DataGridLoggerProvider(loggerFactory));
            }
            else
            {
                services.AddSingleton<IDataGridLoggerProvider>(provider =>
                {
                    var factory = provider.GetService<ILoggerFactory>();
                    return new DataGridLoggerProvider(factory);
                });
            }

            // Register core services
            services.AddScoped<IDataService, DataService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IClipboardService, ClipboardService>();
            services.AddScoped<IColumnService, ColumnService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<INavigationService, NavigationService>();

            // Register ViewModels
            services.AddTransient<AdvancedDataGridViewModel>();

            return services;
        }

        /// <summary>
        /// Registruje služby pre testovanie (s null logger provider)
        /// </summary>
        public static IServiceCollection AddAdvancedDataGridForTesting(this IServiceCollection services)
        {
            services.AddSingleton<IDataGridLoggerProvider, NullDataGridLoggerProvider>();

            // Register services
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