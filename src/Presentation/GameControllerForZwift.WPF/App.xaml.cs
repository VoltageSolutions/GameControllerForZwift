﻿using GameControllerForZwift.Core;
using GameControllerForZwift.Gamepad.DirectInput;
using GameControllerForZwift.Logic;
using GameControllerForZwift.UI.WPF.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using GameControllerForZwift.Gamepad.USB;
using SharpDX.DirectInput;
using GameControllerForZwift.WPF.Logging;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.Views;
using GameControllerForZwift.UI.WPF;

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
            //var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

            // temp changes because i broke DI

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(new CustomMessageEventLogLoggerProvider());
            });
            var mainWindowlogger = loggerFactory.CreateLogger<MainWindowViewModel>();
            ILogger<DirectInputService> inputLogger = loggerFactory.CreateLogger<DirectInputService>();
            

            var fileService = new FileService();
            string json = fileService.ReadFileContent("./DeviceMap.json");
            var deviceLookup = new DeviceLookup(json);
            Func<DeviceInstance, IJoystick> joystickFactory = (device) => new JoystickWrapper(new DirectInput(), device.InstanceGuid);


            INavigationService navigationService = new NavigationService(ServiceProvider);

            var inputService = new DirectInputService(inputLogger, deviceLookup, joystickFactory);
            var dataIntegrator = new DataIntegrator(inputService);
            var mainWindowViewModel = new MainWindowViewModel(navigationService, mainWindowlogger);


            
            
            var mainWindow = new MainWindow(mainWindowViewModel, navigationService);



            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            //services.AddLogging(configure =>
            //{
            //    configure.AddDebug();
            //    configure.AddEventLog();
            //});

            // Register other services as needed
            
            // GameControllerForZwift.Gamepad
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDeviceLookup, DeviceLookup>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<HomePage>();
            services.AddSingleton<SettingsPage>();
            //services.AddSingleton<AnotherPage>();
            services.AddSingleton<ControllerSetupViewModel>();
            services.AddSingleton<ControllerSetupPage>();

            // Register the delegate
            //services.AddSingleton<Func<DeviceInstance, IJoystick>>(serviceProvider =>
            //{
            //    return (device) => new JoystickWrapper(new DirectInput(), device.InstanceGuid);
            //});

            services.AddSingleton<IInputService, DirectInputService>();

            // GameControllerForZwift.Keyboard

            // GameControllerForZwift.Logic
            services.AddSingleton<DataIntegrator>(); // this should have an interface so we can unit test it later


            // GameControllerForZwift.UI.WPF
            // ViewModels
            //services.AddSingleton<MainWindowViewModel>();

            // Register MainWindow with DI so it can receive MainWindowViewModel
            //services.AddTransient<MainWindow>();
        }
    }

}
