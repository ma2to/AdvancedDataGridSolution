
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RpaWpfComponents.AdvancedDataGrid.Configuration;

namespace DemoApplication
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add AdvancedDataGrid services
            services.AddAdvancedDataGrid();

            var serviceProvider = services.BuildServiceProvider();

            // Configure DI for the component
            DependencyInjectionConfig.ConfigureServices(serviceProvider);
        }
    }
}