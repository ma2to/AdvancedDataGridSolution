// ===========================================
// Configuration/DependencyInjectionConfig.cs
// ===========================================
using Microsoft.Extensions.DependencyInjection;
using Components.AdvancedDataGrid.Services.Interfaces;
using Components.AdvancedDataGrid.Services.Implementation;
using Components.AdvancedDataGrid.ViewModels;
using System;

namespace Components.AdvancedDataGrid.Configuration
{
    public static class DependencyInjectionConfig
    {
        private static IServiceProvider _serviceProvider;

        public static void ConfigureServices(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T GetService<T>()
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Services not configured. Call ConfigureServices first.");

            return _serviceProvider.GetService<T>();
        }

        public static T GetRequiredService<T>()
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Services not configured. Call ConfigureServices first.");

            return _serviceProvider.GetRequiredService<T>();
        }

        // Fallback method pre manuálne vytvorenie služieb
        public static IServiceCollection CreateDefaultServices()
        {
            var services = new ServiceCollection();
            services.AddAdvancedDataGrid();
            return services;
        }

        // Factory method pre vytvorenie ViewModel bez DI
        public static AdvancedDataGridViewModel CreateViewModelWithoutDI()
        {
            var dataService = new DataService();
            var validationService = new ValidationService();
            var clipboardService = new ClipboardService();
            var columnService = new ColumnService();
            var exportService = new ExportService();
            var navigationService = new NavigationService();

            return new AdvancedDataGridViewModel(
                dataService,
                validationService,
                clipboardService,
                columnService,
                exportService,
                navigationService);
        }
    }
}