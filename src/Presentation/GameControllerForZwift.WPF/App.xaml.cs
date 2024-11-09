using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput;
using GameControllerForZwift.Gamepad.WinRT;
using GameControllerForZwift.Logic;
using GameControllerForZwift.UI.WPF.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace GameControllerForZwift
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure the service collection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Build the service provider
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Show the main window
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure =>
            {
                configure.AddDebug();
                configure.AddEventLog();
            });

            // Register other services as needed
            services.AddSingleton<IInputService, DirectInputService>();
            //services.AddSingleton<IInputService, GamepadService>();
            services.AddSingleton<DataIntegrator>(); // this should have an interface so we can unit test it later


            // Register ViewModels
            services.AddSingleton<MainWindowViewModel>();

            // Register MainWindow with DI so it can receive MainWindowViewModel
            services.AddTransient<MainWindow>();
        }
    }

}
