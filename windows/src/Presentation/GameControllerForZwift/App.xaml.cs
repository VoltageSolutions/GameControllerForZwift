using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
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
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.Theme;
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

            // Color tokens for the appearance, before the first frame (SCR-SET-001: Match Windows by default).
            ServiceProvider.GetRequiredService<ThemeService>().Apply(Appearance.MatchWindows);

            // Show the main window
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Refresh configuration lists when files change outside the app (CFG-006). Created here, on the UI thread, so
            // its notifications are posted back to this thread.
            ServiceProvider.GetRequiredService<ConfigurationFolderWatcher>().Start();

            // Lists controllers as they connect and disconnect (CTRL-002). Started here, on the UI thread, which owns SDL.
            ServiceProvider.GetRequiredService<ControllerSetupViewModel>().StartControllerDetection();

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
                // Stop asking SDL for controllers before the input service shuts SDL down.
                ServiceProvider?.GetService<ControllerSetupViewModel>()?.StopControllerDetection();

                if (ServiceProvider is IAsyncDisposable provider)
                {
                    // Disposes ZwiftRideOutputService (stops mDNS + Direct Connect server) among other singletons.
                    Task.Run(async () => await provider.DisposeAsync()).Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex) when (ex is AggregateException or ObjectDisposedException or InvalidOperationException)
            {
                // Best effort on shutdown: nothing useful can be done (or logged, the logger may be disposed) at this point.
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
                configure.AddFileLogger(Path.Join(AppContext.BaseDirectory, "logs"));
                configure.AddFilter<FileLoggerProvider>("GameControllerForZwift.Network", LogLevel.Debug);
                // Logs when each controller removal is detected and how long SDL took to report it.
                configure.AddFilter<FileLoggerProvider>("GameControllerForZwift.Gamepad.SDL2.SDL2InputService", LogLevel.Debug);
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
            services.AddSingleton<IZwiftConnection>(provider => provider.GetRequiredService<ZwiftRideOutputService>());

            // Saved configurations (docs/spec/features/saved-configurations.md)
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<ControllerModelMatcher>();
            services.AddSingleton<IConfigurationStore>(provider => new ConfigurationStore(
                provider.GetRequiredService<IFileService>(),
                AppDataPaths.ConfigurationsFolder,
                provider.GetRequiredService<ILogger<ConfigurationStore>>(),
                provider.GetRequiredService<TimeProvider>()));
            services.AddSingleton<IAppSettingsStore>(provider => new AppSettingsStore(
                provider.GetRequiredService<IFileService>(),
                AppDataPaths.SettingsFile,
                provider.GetRequiredService<ILogger<AppSettingsStore>>()));
            services.AddSingleton(provider => new ConfigurationFolderWatcher(
                provider.GetRequiredService<IConfigurationStore>(),
                SynchronizationContext.Current ?? new System.Windows.Threading.DispatcherSynchronizationContext(Current.Dispatcher),
                TimeSpan.FromMilliseconds(400),
                provider.GetRequiredService<ILogger<ConfigurationFolderWatcher>>()));
            services.AddSingleton<IDialogService, DialogService>();

            services.AddSingleton<IControllerProfileService>(provider =>
            {
                var fileService = provider.GetRequiredService<IFileService>();
                string appDirectory = AppContext.BaseDirectory;
                string filePath = Path.Join(appDirectory, "defaultprofile.json");
                //string filePath = "./defaultprofile.json"; // Or fetch this from configuration
                return new ControllerProfileService(fileService, filePath);
            });


            // GameControllerForZwift.Logic
            services.AddSingleton<IDataIntegrator>(provider => new DataIntegrator(
                provider.GetRequiredService<IInputService>(),
                provider.GetRequiredService<IControllerProfileService>(),
                provider.GetRequiredService<IOutputService>()));


            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ThemeService>();

            // GameControllerForZwift.UI.WPF
            // ViewModels. The page view models are singletons on purpose: navigation resolves them from the container, so
            // their state (for example the selected controller) is kept when the user leaves a page and comes back.
            // Views are not registered: the ViewCache in MainWindow creates one view per page view model.
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ZwiftStatus>();
            services.AddSingleton<RideViewModel>();
            services.AddSingleton<ConnectionsViewModel>();
            services.AddSingleton<StyleGalleryViewModel>();
            services.AddSingleton<AboutPageViewModel>(provider =>
            {
                var fileService = provider.GetRequiredService<IFileService>();
                string appDirectory = AppContext.BaseDirectory;
                string filePath = Path.Join(appDirectory, "ACKNOWLEDGEMENTS.md");
                return new AboutPageViewModel(fileService, filePath);
            });
            services.AddSingleton<ControllerSetupViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddTransient<ZwiftFunctionSelectorViewModel>();

            // Register MainWindow with DI so it can receive MainWindowViewModel
            services.AddTransient<InputMapper>();
            services.AddTransient<MainWindow>();
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start the Zwift Ride output.")]
        private static partial void LogStartFailed(ILogger logger, Exception exception);
    }

}
