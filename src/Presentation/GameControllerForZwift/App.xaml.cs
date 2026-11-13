using GameControllerForZwift.Core;
using GameControllerForZwift.Core.FileSystem;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.Gamepad.Mapping;
using GameControllerForZwift.Gamepad.SDL2;
using GameControllerForZwift.Keyboard;
using GameControllerForZwift.Network;
using GameControllerForZwift.Network.Dircon;
using GameControllerForZwift.Network.Logging;
using GameControllerForZwift.Network.Mdns;
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

            // Start the Zwift LAN controller (DirCon server + mDNS advertisement) in the background. It always runs; the
            // "Zwift Controller Emulator" checkbox only decides whether controller input is sent to it.
            {
                var network = ServiceProvider.GetRequiredService<NetworkService>();
                var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await network.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to start the Zwift LAN controller.");
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
                    // Disposes NetworkService (stops mDNS + DirCon server) among other singletons.
                    Task.Run(async () => await provider.DisposeAsync()).Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // Best effort on shutdown.
            }
            base.OnExit(e);
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
                configure.SetMinimumLevel(LogLevel.Information);
                configure.AddFileLogger(Path.Combine(AppContext.BaseDirectory, "logs"));
                configure.AddFilter<GameControllerForZwift.Network.Logging.FileLoggerProvider>("GameControllerForZwift.Network", LogLevel.Debug);
            });

            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IInputService, SDL2InputService>();

            // Output: keyboard + Zwift LAN controller, routed by CompositeOutputService (see its docs for the rules).
            // Choices are loaded from %APPDATA%\GameControllerForZwift\settings.json and saved whenever they change.
            services.AddSingleton<OutputOptions>(_ =>
            {
                var options = new OutputOptions();
                var store = new OutputOptionsStore(OutputOptionsStore.DefaultPath);
                store.Load(options);
                options.Changed += (_, _) => store.Save(options);
                return options;
            });

            // GameControllerForZwift.Keyboard
            services.AddSingleton<KeyboardService>();

            // GameControllerForZwift.Network: the bcride preset (BikeControl replica) for both the DirCon server and the mDNS advertisement.
            var zwiftProfile = DeviceProfiles.ZwiftRidePreset();
            services.AddSingleton(DeviceIdentity.Default with { Name = "Voltage Emulator" });
            services.AddSingleton<IZwiftControllerLink>(provider =>
                new DirconServer(provider.GetRequiredService<DeviceIdentity>(), provider.GetRequiredService<ILogger<DirconServer>>(), profile: zwiftProfile));
            services.AddSingleton(provider => new MdnsAdvertiser(provider.GetRequiredService<ILogger<MdnsAdvertiser>>(), zwiftProfile));
            services.AddSingleton<NetworkService>();
            services.AddSingleton<ILanOutputService>(provider => provider.GetRequiredService<NetworkService>());

            // One composite instance serves as both the discrete (IOutputService) and the continuous (IZwiftControllerOutput) output.
            services.AddSingleton(provider => new CompositeOutputService(
                provider.GetRequiredService<KeyboardService>(),
                provider.GetRequiredService<ILanOutputService>(),
                provider.GetRequiredService<OutputOptions>()));
            services.AddSingleton<IOutputService>(provider => provider.GetRequiredService<CompositeOutputService>());
            services.AddSingleton<IZwiftControllerOutput>(provider => provider.GetRequiredService<CompositeOutputService>());

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
                provider.GetRequiredService<CompositeOutputService>(),
                provider.GetRequiredService<IControllerProfileService>(),
                10,
                provider.GetRequiredService<CompositeOutputService>()));


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
