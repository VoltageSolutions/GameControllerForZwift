using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.Tests.ViewModels;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using GameControllerForZwift.UnitTests;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.Views
{
    /// <summary>
    /// The configuration drop-down as the user drives it: items are picked with the mouse in the real ComboBox of the real
    /// page, dialogs run a nested message loop like a modal window, and the assertions read what the ComboBox shows.
    /// </summary>
    [Collection(WpfWindows.Name)]
    public sealed class ConfigurationSelectionHostingTests : IDisposable
    {
        private static readonly ControllerModel OneS = new(0x045E, 0x02E0, "Xbox One S Controller");

        private readonly ConfigurationTestContext _context = new();

        public ConfigurationSelectionHostingTests()
        {
            // A modal dialog runs a message loop, so queued dispatcher work runs while it is open.
            _context.Dialogs.WhileOpen = PumpNestedFrame;
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public void CFG040_OtherControllersConfiguration_Cancel_DropDownShowsThePreviousConfiguration()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Start is A", OneS);
                _context.Seed("Start is A-yay", Xbox);
                _context.Settings.SetShowOtherControllersConfigurations(true);
                using ServiceProvider provider = CreateProvider(Controller(OneS));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Start is A", window);
                    Assert.Equal("Start is A", viewModel.SelectedConfiguration?.Name);

                    _context.Dialogs.ConfirmAnswers.Enqueue(false);
                    PickWithMouse(configurations, "Start is A-yay (Xbox Series X Controller)", window);

                    Assert.Single(_context.Dialogs.Confirmations);
                    Assert.Equal("Start is A", viewModel.SelectedConfiguration?.Name);
                    AssertShows(configurations, viewModel.SelectedConfiguration);
                    Assert.Equal("Start is A", configurations.Text);
                    Assert.Single(_context.ConfigurationFiles(), f => f.Contains("Start-is-A-yay", StringComparison.Ordinal));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void CFG024_UnsavedChanges_Cancel_DropDownShowsTheEditedConfiguration()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Climbing", Xbox);
                _context.Seed("Hills", Xbox);
                using ServiceProvider provider = CreateProvider(Controller(Xbox));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Climbing", window);
                    viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                    Pump(window);

                    _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);
                    PickWithMouse(configurations, "Hills", window);

                    Assert.Equal(1, _context.Dialogs.UnsavedChangesPrompts);
                    Assert.Equal("Climbing", viewModel.SelectedConfiguration?.Name);
                    Assert.True(viewModel.IsDirty);
                    Assert.Equal("Climbing •", viewModel.SelectedConfiguration?.DisplayName);
                    AssertShows(configurations, viewModel.SelectedConfiguration);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void CFG042_OtherControllersConfiguration_UnsavedChangesCancel_DropDownShowsTheEditedConfiguration()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Start is A", OneS);
                _context.Seed("Start is A-yay", Xbox);
                _context.Settings.SetShowOtherControllersConfigurations(true);
                using ServiceProvider provider = CreateProvider(Controller(OneS));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Start is A", window);
                    viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                    Pump(window);

                    _context.Dialogs.ConfirmAnswers.Enqueue(true);
                    _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);
                    PickWithMouse(configurations, "Start is A-yay (Xbox Series X Controller)", window);

                    Assert.Equal(1, _context.Dialogs.UnsavedChangesPrompts);
                    Assert.Equal("Start is A", viewModel.SelectedConfiguration?.Name);
                    Assert.Equal("Start is A •", viewModel.SelectedConfiguration?.DisplayName);
                    AssertShows(configurations, viewModel.SelectedConfiguration);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void CFG040_OtherControllersConfiguration_UseIt_DropDownShowsTheCopy()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Start is A", OneS);
                _context.Seed("Start is A-yay", Xbox);
                _context.Settings.SetShowOtherControllersConfigurations(true);
                using ServiceProvider provider = CreateProvider(Controller(OneS));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");

                    _context.Dialogs.ConfirmAnswers.Enqueue(true);
                    PickWithMouse(configurations, "Start is A-yay (Xbox Series X Controller)", window);

                    var copy = Assert.IsType<ConfigurationItem>(configurations.SelectedItem);
                    Assert.Same(viewModel.SelectedConfiguration, copy);
                    AssertShows(configurations, copy);
                    Assert.False(copy.IsFromOtherController);
                    Assert.Equal("Start is A-yay", configurations.Text);
                    Assert.Equal(["Default", "Start is A", "Start is A-yay", "Start is A-yay (Xbox Series X Controller)"],
                        configurations.Items.Cast<ConfigurationItem>().Select(i => i.DisplayName));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void CFG024_UnsavedChanges_Save_DropDownShowsThePickedConfiguration()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Climbing", Xbox);
                _context.Seed("Hills", Xbox);
                using ServiceProvider provider = CreateProvider(Controller(Xbox));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Climbing", window);
                    viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                    Pump(window);

                    _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Save);
                    PickWithMouse(configurations, "Hills", window);

                    Assert.Equal("Hills", viewModel.SelectedConfiguration?.Name);
                    AssertShows(configurations, viewModel.SelectedConfiguration);
                    Assert.Equal("Hills", configurations.Text);
                    Assert.Equal(["Default", "Climbing", "Hills"], configurations.Items.Cast<ConfigurationItem>().Select(i => i.DisplayName));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void CTRL008_LastControllerDisconnects_UnsavedChangesCancel_KeepsTheEditsAndTheirConfigurationList()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Start is A", OneS);
                using ServiceProvider provider = CreateProvider(Controller(OneS));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Start is A", window);
                    viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                    Pump(window);

                    _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);
                    _context.DataIntegrator.GetControllers().Returns([]);
                    viewModel.DetectControllers();
                    Pump(window);

                    Assert.Null(viewModel.SelectedController);
                    Assert.Empty(Element<ComboBox>(page, "ControllerComboBox").Items);
                    Assert.Equal(["Default", "Start is A •"], configurations.Items.Cast<ConfigurationItem>().Select(i => i.DisplayName));
                    AssertShows(configurations, viewModel.SelectedConfiguration);
                    Assert.True(Element<Button>(page, "SaveConfigurationAsButton").IsEnabled);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void CTRL008_ControllerSwitchCanceled_ControllerDropDownShowsTheSelectedController()
        {
            // CFG-024 for a controller the user picks: Cancel keeps the controller and its configuration.
            TestApplication.Run(() =>
            {
                _context.Seed("Climbing", Xbox);
                var seriesX = Controller(Xbox);
                var oneS = Controller(OneS);
                using ServiceProvider provider = CreateProvider(seriesX, oneS);
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var controllers = Element<ComboBox>(page, "ControllerComboBox");
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Climbing", window);
                    viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                    Pump(window);

                    _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Cancel);
                    PickWithMouse(controllers, oneS, window);

                    Assert.Equal(1, _context.Dialogs.UnsavedChangesPrompts);
                    Assert.Same(seriesX, viewModel.SelectedController);
                    AssertShows(controllers, seriesX);
                    Assert.Equal("Xbox Series X Controller", controllers.Text);
                    Assert.Equal("Climbing •", viewModel.SelectedConfiguration?.DisplayName);
                    AssertShows(configurations, viewModel.SelectedConfiguration);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void CTRL006_AllControllersDisconnect_ConfigurationBarShowsOnlyDefaultAndDisablesActions(bool unsavedEdits, bool oneAtATime)
        {
            // CTRL-006, CFG-020, CFG-037: with no controller selected only Default is listed, and only Open folder applies.
            TestApplication.Run(() =>
            {
                _context.Seed("Start is A", OneS);
                _context.Seed("Start is A-yay", Xbox);
                _context.Settings.SetShowOtherControllersConfigurations(true);
                var oneS = Controller(OneS);
                var seriesX = Controller(Xbox);
                using ServiceProvider provider = CreateProvider(oneS, seriesX);
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Start is A", window);
                    if (unsavedEdits)
                    {
                        viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                        Pump(window);
                        _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Discard);
                        _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Discard);
                    }

                    if (oneAtATime)
                    {
                        _context.DataIntegrator.GetControllers().Returns([seriesX]);
                        viewModel.DetectControllers();
                        Pump(window);
                    }

                    _context.DataIntegrator.GetControllers().Returns([]);
                    viewModel.DetectControllers();
                    Pump(window);

                    AssertEmptyState(page, viewModel, window);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CTRL006_AfterACanceledPick_AllControllersDisconnect_ConfigurationBarShowsOnlyDefault(bool oneAtATime)
        {
            // The manual test: a configuration of another controller is refused, a mapping changes, then every controller turns off.
            TestApplication.Run(() =>
            {
                _context.Seed("Start is A", OneS);
                _context.Seed("Start is A-yay", Xbox);
                _context.Settings.SetShowOtherControllersConfigurations(true);
                var oneS = Controller(OneS);
                var seriesX = Controller(Xbox);
                using ServiceProvider provider = CreateProvider(oneS, seriesX);
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Start is A", window);
                    _context.Dialogs.ConfirmAnswers.Enqueue(false);
                    PickWithMouse(configurations, "Start is A-yay (Xbox Series X Controller)", window);
                    viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                    Pump(window);
                    _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Discard);
                    _context.Dialogs.UnsavedChangesAnswers.Enqueue(UnsavedChangesChoice.Discard);

                    if (oneAtATime)
                    {
                        _context.DataIntegrator.GetControllers().Returns([oneS]);
                        viewModel.DetectControllers();
                        Pump(window);
                    }

                    _context.DataIntegrator.GetControllers().Returns([]);
                    viewModel.DetectControllers();
                    Pump(window);

                    AssertEmptyState(page, viewModel, window);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void CTRL006_AllControllersDisconnectWhileTheDropDownIsOpen_ConfigurationBarShowsOnlyDefault()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Start is A", OneS);
                _context.Seed("Start is A-yay", Xbox);
                _context.Settings.SetShowOtherControllersConfigurations(true);
                using ServiceProvider provider = CreateProvider(Controller(OneS));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
                    PickWithMouse(configurations, "Start is A", window);
                    configurations.IsDropDownOpen = true;
                    Pump(window);

                    _context.DataIntegrator.GetControllers().Returns([]);
                    viewModel.DetectControllers();
                    Pump(window);
                    configurations.IsDropDownOpen = false;
                    Pump(window);

                    AssertEmptyState(page, viewModel, window);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Asserts that the ComboBox shows the item: it is the selected item, the selected index and the item in the selection box.
        /// </summary>
        private static void AssertShows(ComboBox comboBox, object? item)
        {
            Assert.NotNull(item);
            Assert.Same(item, comboBox.SelectedItem);
            Assert.Equal(comboBox.Items.IndexOf(item), comboBox.SelectedIndex);
            Assert.Same(item, comboBox.SelectionBoxItem);
        }

        private static void AssertEmptyState(ControllerSetupPage page, ControllerSetupViewModel viewModel, Window window)
        {
            var controllers = Element<ComboBox>(page, "ControllerComboBox");
            var configurations = Element<ComboBox>(page, "ConfigurationComboBox");
            Assert.Null(viewModel.SelectedController);
            Assert.Empty(controllers.Items);
            Assert.Null(controllers.SelectedItem);
            Assert.Equal(Visibility.Visible, Element<TextBlock>(page, "NoControllerText").Visibility);

            var item = Assert.IsType<ConfigurationItem>(Assert.Single(configurations.Items.Cast<object?>()));
            Assert.True(item.IsDefault);
            AssertShows(configurations, item);
            Assert.Same(item, viewModel.SelectedConfiguration);
            Assert.Equal("Default", configurations.Text);
            Assert.False(viewModel.IsDirty);

            Assert.False(Element<Button>(page, "SaveConfigurationButton").IsEnabled);
            Assert.False(Element<Button>(page, "SaveConfigurationAsButton").IsEnabled);
            Assert.False(Element<Button>(page, "DeleteConfigurationButton").IsEnabled);

            var more = Element<Button>(page, "MoreConfigurationActionsButton");
            more.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, more));
            Pump(window);
            var menu = more.ContextMenu!;
            try
            {
                Assert.False(MenuItem(menu, "ImportConfigurationMenuItem").IsEnabled);
                Assert.False(MenuItem(menu, "RenameConfigurationMenuItem").IsEnabled);
                Assert.True(MenuItem(menu, "OpenConfigurationFolderMenuItem").IsEnabled);
            }
            finally
            {
                menu.IsOpen = false;
            }
        }

        private ServiceProvider CreateProvider(params IController[] controllers)
        {
            _context.DataIntegrator.GetControllers().Returns(controllers.ToList());

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(_context.DataIntegrator);
            services.AddSingleton(_context.ProfileService);
            services.AddSingleton<IConfigurationStore>(_context.Store);
            services.AddSingleton<IAppSettingsStore>(_context.Settings);
            services.AddSingleton<IDialogService>(_context.Dialogs);
            services.AddSingleton(_context.Matcher);
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ControllerSetupViewModel>();
            services.AddSingleton(new AboutPageViewModel("# Acknowledgements"));
            services.AddSingleton<SettingsViewModel>();
            services.AddTransient<MainWindow>();
            return services.BuildServiceProvider();
        }

        private static MainWindow ShowWindow(ServiceProvider provider)
        {
            var window = provider.GetRequiredService<MainWindow>();
            window.ShowActivated = false;
            window.ShowInTaskbar = false;
            window.Show();
            Pump(window);
            return window;
        }

        private static ControllerSetupPage ShowControllerSetup(MainWindow window)
        {
            window.ViewModel.NavigateToPage(window.ViewModel.Pages.Single(p => p.ViewModelType == typeof(ControllerSetupViewModel)));
            Pump(window);
            return Assert.IsType<ControllerSetupPage>(((ContentControl)window.FindName("RootContent")).Content);
        }

        private static void Pump(Window window)
        {
            for (int i = 0; i < 6; i++)
            {
                window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
            }
        }

        private static void PumpNestedFrame()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
        }

        private static T Element<T>(FrameworkElement page, string name) where T : class => Assert.IsType<T>(page.FindName(name));

        private static MenuItem MenuItem(ContextMenu menu, string automationId) =>
            menu.Items.OfType<MenuItem>().Single(i => System.Windows.Automation.AutomationProperties.GetAutomationId(i) == automationId);

        /// <summary>
        /// Opens the drop-down and releases the mouse button over the entry whose text is shown, as a click does.
        /// </summary>
        private static void PickWithMouse(ComboBox comboBox, string text, Window window) =>
            PickWithMouse(comboBox, comboBox.Items.Cast<object>().Single(i => string.Equals(TextOf(comboBox, i), text, StringComparison.Ordinal)), window);

        private static void PickWithMouse(ComboBox comboBox, object item, Window window)
        {
            comboBox.IsDropDownOpen = true;
            Pump(window);
            var container = Assert.IsType<ComboBoxItem>(comboBox.ItemContainerGenerator.ContainerFromItem(item));
            container.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent,
            });
            Pump(window);
        }

        private static string? TextOf(ComboBox comboBox, object item) => item switch
        {
            ConfigurationItem configuration => configuration.DisplayName,
            IController controller => controller.Name,
            _ => comboBox.DisplayMemberPath,
        };
    }
}
