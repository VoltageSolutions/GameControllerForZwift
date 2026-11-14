using GameControllerForZwift.Core;
using GameControllerForZwift.Core.FileSystem;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.Gamepad.Mapping;
using GameControllerForZwift.Gamepad.SDL2;
using GameControllerForZwift.Network.DirectConnect;
using GameControllerForZwift.Network.Mdns;
using GameControllerForZwift.Network.Output;
using GameControllerForZwift.Network.ZwiftRide;
using GameControllerForZwift.UI.WPF;
using GameControllerForZwift.UI.WPF.Controls;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using GameControllerForZwift.WPF.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
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

            // Start the Zwift Ride output (Direct Connect server + mDNS advertisement) in the background. It always runs and is the only output.
            {
                var output = ServiceProvider.GetRequiredService<ZwiftRideOutputService>();
                var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await output.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        LogStartFailed(logger, ex);
                    }
                });
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (ServiceProvider is IAsyncDisposable provider)
                {
                    // Disposes ZwiftRideOutputService (stops mDNS + Direct Connect server) among other singletons.
                    Task.Run(async () => await provider.DisposeAsync()).Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // Best effort on shutdown.
            }
            base.OnExit(e);
        }

        private static bool IsTestEnvironment()
        {
            return AppDomain.CurrentDomain.FriendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase);
        }

        private static void ConfigureServices(IServiceCollection services)
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
                configure.SetMinimumLevel(LogLevel.Information);
                configure.AddFileLogger(Path.Combine(AppContext.BaseDirectory, "logs"));
                configure.AddFilter<FileLoggerProvider>("GameControllerForZwift.Network", LogLevel.Debug);
            });

            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IInputService, SDL2InputService>();

            // GameControllerForZwift.Network: the Zwift Ride output (Direct Connect server + mDNS advertisement).
            services.AddSingleton(DeviceIdentity.Default with { Name = "Voltage Gamepad" });
            services.AddSingleton<IZwiftRideLink>(provider =>
                new DirectConnectServer(provider.GetRequiredService<ILogger<DirectConnectServer>>()));
            services.AddSingleton(provider => new MdnsAdvertiser(provider.GetRequiredService<ILogger<MdnsAdvertiser>>()));
            services.AddSingleton<ZwiftRideOutputService>();
            services.AddSingleton<IOutputService>(provider => provider.GetRequiredService<ZwiftRideOutputService>());

            services.AddSingleton<IControllerProfileService>(provider =>
            {
                var fileService = provider.GetRequiredService<IFileService>();
                string appDirectory = AppContext.BaseDirectory;
                string filePath = Path.Combine(appDirectory, "defaultprofile.json");
                //string filePath = "./defaultprofile.json"; // Or fetch this from configuration
                return new ControllerProfileService(fileService, filePath);
            });


            // GameControllerForZwift.Logic
            services.AddSingleton<IDataIntegrator>(provider => new DataIntegrator(
                provider.GetRequiredService<IInputService>(),
                provider.GetRequiredService<IControllerProfileService>(),
                provider.GetRequiredService<IOutputService>()));


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

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start the Zwift Ride output.")]
        private static partial void LogStartFailed(ILogger logger, Exception exception);
    }

}
