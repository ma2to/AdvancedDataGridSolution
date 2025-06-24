// RpaWpfComponents/AdvancedDataGrid/Configuration/DependencyInjectionConfig.cs
using RpaWpfComponents.AdvancedDataGrid.Configuration;
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

            // Configure the static logger factory
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            LoggerFactory.Configure(loggerFactory);
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

        public static IServiceCollection CreateDefaultServices()
        {
            var services = new ServiceCollection();
            services.AddAdvancedDataGrid();
            return services;
        }

        public static AdvancedDataGridViewModel CreateViewModelWithoutDI()
        {
            var loggerFactory = _serviceProvider?.GetService<ILoggerFactory>();

            var dataService = new DataService(loggerFactory?.CreateLogger<DataService>());
            var validationService = new ValidationService(loggerFactory?.CreateLogger<ValidationService>());
            var clipboardService = new ClipboardService(loggerFactory?.CreateLogger<ClipboardService>());
            var columnService = new ColumnService(loggerFactory?.CreateLogger<ColumnService>());
            var exportService = new ExportService(loggerFactory?.CreateLogger<ExportService>());
            var navigationService = new NavigationService(loggerFactory?.CreateLogger<NavigationService>());

            return new AdvancedDataGridViewModel(
                dataService,
                validationService,
                clipboardService,
                columnService,
                exportService,
                navigationService,
                loggerFactory?.CreateLogger<AdvancedDataGridViewModel>());
        }
    }
}