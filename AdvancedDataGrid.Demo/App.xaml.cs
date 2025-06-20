using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Components.AdvancedDataGrid.Configuration;

namespace AdvancedDataGrid.Demo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure services for the entire application
            var services = new ServiceCollection();
            services.AddAdvancedDataGrid();
            var serviceProvider = services.BuildServiceProvider();

            // Configure DI for the component
            DependencyInjectionConfig.ConfigureServices(serviceProvider);
        }
    }
}