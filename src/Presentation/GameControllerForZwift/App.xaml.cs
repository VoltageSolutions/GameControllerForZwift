﻿using GameControllerForZwift.Core;
using GameControllerForZwift.Core.FileSystem;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.Gamepad.Mapping;
using GameControllerForZwift.Gamepad.SDL2;
using GameControllerForZwift.Keyboard;
using GameControllerForZwift.UI.WPF;
using GameControllerForZwift.UI.WPF.Controls;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using GameControllerForZwift.WPF.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
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
            if (IsTestEnvironment())
            {
                return; // Skip startup logic during tests
            }

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

        private bool IsTestEnvironment()
        {
            return AppDomain.CurrentDomain.FriendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase);
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

            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IInputService, SDL2InputService>();

            // GameControllerForZwift.Keyboard
            services.AddSingleton<IOutputService, KeyboardService>();
            services.AddSingleton<IControllerProfileService>(provider =>
            {
                var fileService = provider.GetRequiredService<IFileService>();
                string appDirectory = AppContext.BaseDirectory;
                string filePath = Path.Combine(appDirectory, "defaultprofile.json");
                //string filePath = "./defaultprofile.json"; // Or fetch this from configuration
                return new ControllerProfileService(fileService, filePath);
            });


            // GameControllerForZwift.Logic
            services.AddSingleton<IDataIntegrator, DataIntegrator>(); // this should have an interface so we can unit test it later


            services.AddSingleton<INavigationService, NavigationService>();

            // GameControllerForZwift.UI.WPF
            // ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<AboutPageViewModel>();
            services.AddSingleton<AboutPageViewModel>(provider =>
            {
                var fileService = provider.GetRequiredService<IFileService>();
                string appDirectory = AppContext.BaseDirectory;
                string filePath = Path.Combine(appDirectory, "ACKNOWLEDGEMENTS.md");
                //string filePath = "./ACKNOWLEDGEMENTS.md"; // Or fetch this from configuration
                return new AboutPageViewModel(fileService, filePath);
            });
            services.AddSingleton<ControllerSetupViewModel>();
            services.AddTransient<ZwiftFunctionSelectorViewModel>();

            // Register MainWindow with DI so it can receive MainWindowViewModel
            services.AddTransient<InputMapper>();
            services.AddSingleton<AboutPage>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<ControllerSetupPage>();
            services.AddTransient<MainWindow>();
        }
    }

}
