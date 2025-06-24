// ============================================
// RpaWpfComponents/SmartListBox/Configuration/DependencyInjectionConfig.cs
// ============================================
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.SmartListBox.Services.Implementation;
using RpaWpfComponents.SmartListBox.Services.Interfaces;
using RpaWpfComponents.SmartListBox.ViewModels;

namespace RpaWpfComponents.SmartListBox.Configuration
{
    internal static class DependencyInjectionConfig
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Konfiguruje service provider a logger factory
        /// </summary>
        /// <param name="serviceProvider">Service provider z DI kontajnera</param>
        public static void ConfigureServices(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            LoggerFactory.Configure(loggerFactory);
        }

        /// <summary>
        /// Získa službu z DI kontajnera
        /// </summary>
        /// <typeparam name="T">Typ služby</typeparam>
        /// <returns>Inštancia služby alebo null ak nie je dostupná</returns>
        public static T? GetService<T>()
        {
            if (_serviceProvider == null)
                return default(T);

            try
            {
                return _serviceProvider.GetService<T>();
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Vytvorí SmartListBoxViewModel bez DI kontajnera
        /// </summary>
        /// <param name="loggerFactory">Logger factory pre logovanie</param>
        /// <returns>Nakonfigurovaný SmartListBoxViewModel</returns>
        public static SmartListBoxViewModel CreateViewModelWithoutDI(ILoggerFactory? loggerFactory = null)
        {
            // Vytvor služby manuálne
            var dataService = new DataProcessingService(loggerFactory?.CreateLogger<DataProcessingService>());
            var selectionService = new SelectionService(loggerFactory?.CreateLogger<SelectionService>());

            // Vytvor ViewModel
            return new SmartListBoxViewModel(
                dataService,
                selectionService,
                loggerFactory?.CreateLogger<SmartListBoxViewModel>());
        }
    }
}