// ===========================================
// RpaWpfComponents/AdvancedDataGrid/Configuration/DependencyInjectionConfig.cs - KOMPLETNÁ NÁHRADA
// ===========================================
using RpaWpfComponents.AdvancedDataGrid.Interfaces;
using RpaWpfComponents.AdvancedDataGrid.Services;
using RpaWpfComponents.AdvancedDataGrid.Services.Implementation;
using RpaWpfComponents.AdvancedDataGrid.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace RpaWpfComponents.AdvancedDataGrid.Configuration
{
    public static class DependencyInjectionConfig
    {
        private static IServiceProvider? _serviceProvider;

        public static void ConfigureServices(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T? GetService<T>()
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Services not configured. Call ConfigureServices first.");

            return _serviceProvider.GetService<T>();
        }

        public static T GetRequiredService<T>() where T : notnull
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Services not configured. Call ConfigureServices first.");

            return _serviceProvider.GetRequiredService<T>();
        }

        public static IServiceCollection CreateDefaultServices(ILoggerFactory? loggerFactory = null)
        {
            var services = new ServiceCollection();
            services.AddAdvancedDataGrid(loggerFactory);
            return services;
        }

        public static AdvancedDataGridViewModel CreateViewModelWithoutDI(ILoggerFactory? loggerFactory = null)
        {
            var loggerProvider = new DataGridLoggerProvider(loggerFactory);

            var dataService = new DataService(loggerProvider.CreateLogger<DataService>());
            var validationService = new ValidationService(loggerProvider.CreateLogger<ValidationService>());
            var clipboardService = new ClipboardService(loggerProvider.CreateLogger<ClipboardService>());
            var columnService = new ColumnService(loggerProvider.CreateLogger<ColumnService>());
            var exportService = new ExportService(loggerProvider.CreateLogger<ExportService>());
            var navigationService = new NavigationService(loggerProvider.CreateLogger<NavigationService>());

            return new AdvancedDataGridViewModel(
                dataService,
                validationService,
                clipboardService,
                columnService,
                exportService,
                navigationService,
                loggerProvider.CreateLogger<AdvancedDataGridViewModel>());
        }
    }
}