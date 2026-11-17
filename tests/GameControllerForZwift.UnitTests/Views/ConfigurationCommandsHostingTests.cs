using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;
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
    /// The configuration commands as the app hosts them: MainWindow from the container, the real navigation service and
    /// view cache, and assertions made through the WPF elements the user clicks.
    /// </summary>
    [Collection(WpfWindows.Name)]
    public sealed class ConfigurationCommandsHostingTests : IDisposable
    {
        private readonly ConfigurationTestContext _context = new();

        public void Dispose() => _context.Dispose();

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

        private static void Pump(Window window)
        {
            for (int i = 0; i < 6; i++)
            {
                window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
            }
        }

        private static ControllerSetupPage ShowControllerSetup(MainWindow window)
        {
            window.ViewModel.NavigateToPage(window.ViewModel.Pages.Single(p => p.ViewModelType == typeof(ControllerSetupViewModel)));
            Pump(window);
            return Assert.IsType<ControllerSetupPage>(((ContentControl)window.FindName("RootContent")).Content);
        }

        private static T Element<T>(FrameworkElement page, string name) where T : class => Assert.IsType<T>(page.FindName(name));

        private static ContextMenu OpenMoreActionsMenu(ControllerSetupPage page)
        {
            var button = Element<Button>(page, "MoreConfigurationActionsButton");
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
            var menu = button.ContextMenu!;
            Assert.True(menu.IsOpen);
            return menu;
        }

        private static MenuItem MenuItem(ContextMenu menu, string automationId) =>
            menu.Items.OfType<MenuItem>().Single(i => System.Windows.Automation.AutomationProperties.GetAutomationId(i) == automationId);

        private static void Click(ICommandSource source)
        {
            Assert.NotNull(source.Command);
            Assert.True(((UIElement)source).IsEnabled);
            source.Command.Execute(source.CommandParameter);
        }

        [Fact]
        public void EditingDefault_EnablesSaveAsOnly_AndSaveAsButtonSavesANewConfiguration()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider(Controller(Xbox));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);

                    viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                    Pump(window);

                    var saveAs = Element<Button>(page, "SaveConfigurationAsButton");
                    Assert.Same(viewModel.SaveAsCommand, saveAs.Command);
                    Assert.True(saveAs.IsEnabled);
                    Assert.False(Element<Button>(page, "SaveConfigurationButton").IsEnabled);
                    Assert.False(Element<Button>(page, "DeleteConfigurationButton").IsEnabled);

                    _context.Dialogs.NameAnswers.Enqueue("Climbing");
                    Click(saveAs);
                    Pump(window);

                    Assert.Single(_context.Dialogs.NamePrompts);
                    Assert.Equal("Climbing", viewModel.SelectedConfiguration?.Name);
                    Assert.Same(viewModel.SelectedConfiguration, Element<ComboBox>(page, "ConfigurationComboBox").SelectedItem);
                    Assert.True(Element<Button>(page, "DeleteConfigurationButton").IsEnabled);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ControllerPickedAfterStartup_EnablesSaveAs()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var saveAs = Element<Button>(page, "SaveConfigurationAsButton");
                    Assert.False(saveAs.IsEnabled);

                    IController controller = Controller(Xbox);
                    _context.DataIntegrator.GetControllers().Returns([controller]);
                    viewModel.RefreshControllerListCommand.Execute(null);
                    Pump(window);
                    var controllers = FindDescendants<ComboBox>(page).First(c => ReferenceEquals(c.ItemsSource, viewModel.Controllers));
                    controllers.SelectedItem = controller;
                    viewModel.Selector(ControllerInput.Menu).SelectedZwiftFunction = ZwiftFunction.RideOn;
                    Pump(window);

                    Assert.Same(controller, viewModel.SelectedController);
                    Assert.True(saveAs.IsEnabled);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MoreActionsMenu_ItemsAreBoundToTheViewModelCommands()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider(Controller(Xbox));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);

                    ContextMenu menu = OpenMoreActionsMenu(page);
                    Pump(window);

                    var import = MenuItem(menu, "ImportConfigurationMenuItem");
                    var rename = MenuItem(menu, "RenameConfigurationMenuItem");
                    var openFolder = MenuItem(menu, "OpenConfigurationFolderMenuItem");
                    Assert.Same(viewModel.ImportCommand, import.Command);
                    Assert.Same(viewModel.RenameCommand, rename.Command);
                    Assert.Same(viewModel.OpenFolderCommand, openFolder.Command);
                    Assert.True(import.IsEnabled);
                    Assert.False(rename.IsEnabled);

                    Click(openFolder);

                    Assert.Equal([_context.ConfigurationsFolder], _context.Dialogs.OpenedFolders);
                    menu.IsOpen = false;
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MoreActionsMenu_RenameAndImportReachTheDialogs()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Climbing", Xbox);
                using ServiceProvider provider = CreateProvider(Controller(Xbox));
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    Element<ComboBox>(page, "ConfigurationComboBox").SelectedItem = viewModel.Configurations.Single(c => c.Name == "Climbing");
                    Pump(window);

                    ContextMenu menu = OpenMoreActionsMenu(page);
                    Pump(window);
                    _context.Dialogs.NameAnswers.Enqueue("Hills");
                    Click(MenuItem(menu, "RenameConfigurationMenuItem"));
                    menu.IsOpen = false;
                    Pump(window);

                    Assert.Equal("Hills", viewModel.SelectedConfiguration?.Name);

                    menu = OpenMoreActionsMenu(page);
                    Pump(window);
                    Click(MenuItem(menu, "ImportConfigurationMenuItem"));
                    menu.IsOpen = false;

                    Assert.Equal(1, _context.Dialogs.FilePickPrompts);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void SettingsDeleteAllButton_IsBoundAndDeletesTheFiles()
        {
            TestApplication.Run(() =>
            {
                _context.Seed("Climbing", Xbox);
                using ServiceProvider provider = CreateProvider(Controller(Xbox));
                MainWindow window = ShowWindow(provider);
                try
                {
                    window.ViewModel.SettingsCommand.Execute(null);
                    Pump(window);
                    var page = Assert.IsType<SettingsPage>(((ContentControl)window.FindName("RootContent")).Content);
                    var deleteAll = Element<Button>(page, "DeleteAllConfigurationsButton");
                    Assert.Same(provider.GetRequiredService<SettingsViewModel>().DeleteAllConfigurationsCommand, deleteAll.Command);

                    _context.Dialogs.ConfirmAnswers.Enqueue(true);
                    Click(deleteAll);

                    Assert.Single(_context.Dialogs.Confirmations);
                    Assert.Empty(_context.ConfigurationFiles());
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ControllerConnectsAfterTheDropDownWasOpened_DropDownShowsBothControllers()
        {
            // CTRL-001: the second controller must be visible in the drop-down, not just present in its items.
            TestApplication.Run(() =>
            {
                var seriesX = Controller(Xbox);
                using ServiceProvider provider = CreateProvider(seriesX);
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewModel = provider.GetRequiredService<ControllerSetupViewModel>();
                    ControllerSetupPage page = ShowControllerSetup(window);
                    var controllers = FindDescendants<ComboBox>(page).First(c => ReferenceEquals(c.ItemsSource, viewModel.Controllers));
                    Assert.Same(seriesX, controllers.SelectedItem);
                    OpenAndCloseDropDown(controllers, window);

                    var oneS = TestControllers.Create(0x045E, 0x02E0, "Xbox One S Controller");
                    _context.DataIntegrator.GetControllers().Returns([seriesX, oneS]);
                    viewModel.RefreshControllerList();
                    Pump(window);

                    controllers.IsDropDownOpen = true;
                    Pump(window);
                    var popup = Assert.IsType<Popup>(controllers.Template.FindName("PART_Popup", controllers));
                    var scrollViewer = FindDescendants<ScrollViewer>(popup.Child).First();
                    var rowHeight = ((FrameworkElement)controllers.ItemContainerGenerator.ContainerFromIndex(0)).ActualHeight;
                    double extent = scrollViewer.ExtentHeight;
                    controllers.IsDropDownOpen = false;
                    Pump(window);

                    Assert.Equal(2, controllers.Items.Count);
                    Assert.True(extent >= 2 * rowHeight, $"The drop-down shows {extent} of {2 * rowHeight} for two rows.");
                    Assert.Same(seriesX, controllers.SelectedItem);
                    Assert.Same(seriesX, viewModel.SelectedController);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void OpenAndCloseDropDown(ComboBox comboBox, Window window)
        {
            comboBox.IsDropDownOpen = true;
            Pump(window);
            comboBox.IsDropDownOpen = false;
            Pump(window);
        }

        private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                    yield return match;
                foreach (var descendant in FindDescendants<T>(child))
                    yield return descendant;
            }
        }
    }
}
