using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput;
using GameControllerForZwift.Gamepad.DirectInput.ControllerMapping;
using GameControllerForZwift.Gamepad.FileSystem;
using GameControllerForZwift.Gamepad.SDL2;
using GameControllerForZwift.Gamepad.USB;
using GameControllerForZwift.Logic;
using GameControllerForZwift.UI.WPF;
using GameControllerForZwift.UI.WPF.Controls;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using GameControllerForZwift.WPF.Logging;
using Microsoft.Extensions.DependencyInjection;
using SharpDX.DirectInput;
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
            Current.ThemeMode = ThemeMode.System;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Standard .NET Event Logging
            //services.AddLogging(configure =>
            //{
            //    configure.AddDebug();
            //    configure.AddEventLog();
            //});

            // Register custom event logging
            services.AddLogging(configure =>
            {
                //configure.ClearProviders(); // Optional: Removes default logging providers if you only want your custom one.
                configure.AddCustomEventLogProvider(); // Add your custom provider
            });

            // Register DeviceLookup
            services.AddSingleton<IFileService, FileService>();
            //services.AddSingleton<IDeviceLookup>(provider =>
            //{
            //    var fileService = provider.GetRequiredService<IFileService>();
            //    string filePath = "./DeviceMap.json"; // Or fetch this from configuration
            //    return new DeviceLookup(fileService, filePath);
            //});
            //services.AddSingleton<IControllerMapping>(provider =>
            //{
            //    var fileService = provider.GetRequiredService<IFileService>();
            //    string filePath = "./DeviceButtonMap.json"; // Or fetch this from configuration
            //    return new ControllerMapper(fileService, filePath);
            //});

            //// Register the delegate
            //services.AddSingleton<Func<DeviceInstance, IJoystick>>(serviceProvider =>
            //{
            //    return (device) => new JoystickWrapper(new DirectInput(), device.InstanceGuid);
            //});
            //services.AddSingleton<IInputService, DirectInputService>();


            services.AddSingleton<IInputService, SDL2InputService>();

            // GameControllerForZwift.Keyboard

            // GameControllerForZwift.Logic
            services.AddSingleton<DataIntegrator>(); // this should have an interface so we can unit test it later


            services.AddSingleton<INavigationService, NavigationService>();

            // GameControllerForZwift.UI.WPF
            // ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ControllerSetupViewModel>();
            services.AddTransient<ZwiftFunctionSelectorViewModel>();

            // Register MainWindow with DI so it can receive MainWindowViewModel
            services.AddTransient<InputMapper>();
            services.AddSingleton<HomePage>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<ControllerSetupPage>();
            services.AddTransient<MainWindow>();
        }
    }

}
