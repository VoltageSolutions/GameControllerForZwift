using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GameControllerForZwift.UnitTests
{
    /// <summary>The window shell (docs/spec/ux/navigation.md).</summary>
    [Collection(WpfWindows.Name)]
    public class MainWindowTests
    {
        private static readonly string[] NavIds = ["NavSidebar", "NavRail", "NavBottomBar"];
        private static readonly string[] MenuIds = ["MenuSettingsItem", "MenuHelpItem", "MenuCreditsItem", "MenuSupportItem"];

        private static ServiceProvider CreateProvider()
        {
            var dataIntegrator = Substitute.For<IDataIntegrator>();
            dataIntegrator.GetControllers().Returns(new List<IController>());
            var profileService = Substitute.For<IControllerProfileService>();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(dataIntegrator);
            services.AddSingleton(profileService);
            services.AddSingleton(Substitute.For<IConfigurationStore>());
            services.AddSingleton(Substitute.For<IAppSettingsStore>());
            services.AddSingleton(Substitute.For<IDialogService>());
            services.AddSingleton<ControllerModelMatcher>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ZwiftStatus>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<RideViewModel>();
            services.AddSingleton<ControllerSetupViewModel>();
            services.AddSingleton<ConnectionsViewModel>();
            services.AddSingleton(new AboutPageViewModel("# Acknowledgements"));
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<StyleGalleryViewModel>();
            services.AddTransient<MainWindow>();
            return services.BuildServiceProvider();
        }

        // Shows a window for the test and closes it afterwards.
        private static void WithWindow(Action<ServiceProvider, MainWindow> test, double width = 1600)
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                var window = provider.GetRequiredService<MainWindow>();
                window.ShowActivated = false;
                window.ShowInTaskbar = false;
                window.Width = width;
                window.Show();
                Pump(window);
                try
                {
                    test(provider, window);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        // Runs everything queued so far, including the idle work that prepares the other pages.
        private static void Pump(Window window)
        {
            for (int i = 0; i < 8; i++)
            {
                window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
            }
        }

        private static T Find<T>(MainWindow window, string name) where T : class => Assert.IsAssignableFrom<T>(window.FindName(name));

        private static object? ShownContent(MainWindow window) => Find<ContentControl>(window, "RootContent").Content;

        // Clicks like a user: the click handlers run and so does the command.
        private static void Click(Button button)
        {
            Assert.True(button.IsEnabled);
            var invoke = (System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(button)
                .GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke);
            invoke.Invoke();
            Pump(Window.GetWindow(button));
        }

        private static void PressKey(MainWindow window, Key key) =>
            window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, key)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent,
            });

        [Fact]
        public void Start_ShowsTheButtonsTabWithEmptyHistory()
        {
            WithWindow((provider, window) =>
            {
                var page = Assert.IsType<ControllerSetupPage>(ShownContent(window));
                Assert.Same(provider.GetRequiredService<ControllerSetupViewModel>(), page.DataContext);
                Assert.Equal(ShellTab.Buttons, window.ViewModel.SelectedTab!.Tab);
                Assert.Same(window.ViewModel.SelectedTab, Find<UI.WPF.Controls.NavTabList>(window, "SidebarTabs").SelectedItem);
                Assert.False(window.ViewModel.CanGoBack);
                Assert.Equal(Visibility.Collapsed, Find<Button>(window, "BackButton").Visibility);
            });
        }

        [Theory]
        [InlineData(0, typeof(RidePage))]
        [InlineData(2, typeof(ConnectionsPage))]
        [InlineData(1, typeof(ControllerSetupPage))]
        public void UXN001_SelectingATabShowsItsPage(int index, Type pageType)
        {
            WithWindow((_, window) =>
            {
                var tabs = Find<UI.WPF.Controls.NavTabList>(window, "SidebarTabs");

                tabs.SelectedItem = window.ViewModel.Tabs[index];
                Pump(window);

                Assert.IsType(pageType, ShownContent(window));
                Assert.False(window.ViewModel.CanGoBack);
            });
        }

        [Fact]
        public void UXN008_UXN009_SettingsOpensFromTheMenuAsAPushedScreen()
        {
            WithWindow((_, window) =>
            {
                Click(Find<UI.WPF.Controls.MoreMenuButton>(window, "MoreButton"));
                Pump(window);
                Assert.Equal(Visibility.Visible, Find<Grid>(window, "MoreMenuLayer").Visibility);

                Click(Find<Button>(window, "MenuSettingsItem"));
                Pump(window);

                Assert.IsType<SettingsPage>(ShownContent(window));
                Assert.Equal(Visibility.Collapsed, Find<Grid>(window, "MoreMenuLayer").Visibility);
                Assert.Null(Find<UI.WPF.Controls.NavTabList>(window, "SidebarTabs").SelectedItem);
                Assert.Equal(Visibility.Visible, Find<Button>(window, "BackButton").Visibility);
            });
        }

        [Fact]
        public void UXN016_BackReturnsToTheScreenTheMenuWasOpenedFrom()
        {
            WithWindow((_, window) =>
            {
                window.ViewModel.SelectTab(ShellTab.Connections);
                window.ViewModel.OpenCreditsCommand.Execute(null);
                Pump(window);
                Assert.IsType<AboutPage>(ShownContent(window));

                Click(Find<Button>(window, "BackButton"));
                Pump(window);

                Assert.IsType<ConnectionsPage>(ShownContent(window));
                Assert.Equal(ShellTab.Connections, window.ViewModel.SelectedTab!.Tab);
                Assert.Equal(Visibility.Collapsed, Find<Button>(window, "BackButton").Visibility);
            });
        }

        [Fact]
        public void SelectingATabFromAPushedScreen_ClearsTheHistory()
        {
            WithWindow((_, window) =>
            {
                window.ViewModel.OpenSettingsCommand.Execute(null);
                Assert.True(window.ViewModel.CanGoBack);

                window.ViewModel.SelectTab(ShellTab.Ride);
                Pump(window);

                Assert.IsType<RidePage>(ShownContent(window));
                Assert.False(window.ViewModel.CanGoBack);
            });
        }

        [Fact]
        public void ReturningToAPage_ShowsTheSameViewInstance()
        {
            WithWindow((_, window) =>
            {
                object? first = ShownContent(window);
                window.ViewModel.OpenSettingsCommand.Execute(null);

                window.ViewModel.BackCommand.Execute(null);

                Assert.Same(first, ShownContent(window));
            });
        }

        [Fact]
        public void IdleWork_BuildsThePagesThatHaveNotBeenVisited()
        {
            WithWindow((_, window) =>
            {
                var viewCache = (ViewCache)typeof(MainWindow)
                    .GetField("_viewCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .GetValue(window)!;
                var views = (System.Collections.IDictionary)typeof(ViewCache)
                    .GetField("_views", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .GetValue(viewCache)!;

                Assert.Equal(5, views.Count);
                Assert.True(((FrameworkElement)views[typeof(AboutPageViewModel)]!).IsMeasureValid);
                Assert.Null(((FrameworkElement)views[typeof(SettingsViewModel)]!).Parent);
            });
        }

        [Theory]
        [InlineData(1600, "NavSidebar", NavLayout.Sidebar)]
        [InlineData(800, "NavRail", NavLayout.Rail)]
        [InlineData(500, "NavBottomBar", NavLayout.BottomBar)]
        public void UXN010_UXN011_TheTabsMoveWithTheWindowWidth(double width, string visibleNav, NavLayout layout)
        {
            WithWindow((_, window) =>
            {
                Assert.Equal(layout, window.ViewModel.NavLayout);
                foreach (string nav in NavIds)
                {
                    Assert.Equal(nav == visibleNav ? Visibility.Visible : Visibility.Collapsed, Find<Border>(window, nav).Visibility);
                }

                // UX-N-004: the More button stays at the top right whatever the width.
                var more = Find<Button>(window, "MoreButton");
                Assert.Equal(HorizontalAlignment.Right, more.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Top, more.VerticalAlignment);
            }, width);
        }

        [Fact]
        public void UXN011_ResizingSwitchesThePlacement()
        {
            WithWindow((_, window) =>
            {
                window.Width = 700;
                Pump(window);
                Assert.Equal(NavLayout.Rail, window.ViewModel.NavLayout);

                window.Width = 1200;
                Pump(window);
                Assert.Equal(NavLayout.Sidebar, window.ViewModel.NavLayout);
            });
        }

        [Fact]
        public void BottomBar_KeepsTheContentAboveTheBar()
        {
            WithWindow((_, window) =>
            {
                Assert.Equal(88, Find<Grid>(window, "ContentArea").Margin.Bottom);
            }, 500);
        }

        [Fact]
        public void MoreMenu_EscapeClosesIt()
        {
            WithWindow((_, window) =>
            {
                window.ViewModel.IsMoreMenuOpen = true;
                Pump(window);

                PressKey(window, Key.Escape);

                Assert.False(window.ViewModel.IsMoreMenuOpen);
                Assert.Equal(Visibility.Collapsed, Find<Grid>(window, "MoreMenuLayer").Visibility);
            });
        }

        [Fact]
        public void MoreMenu_EscapeWithTheMenuClosedDoesNothing()
        {
            WithWindow((_, window) =>
            {
                PressKey(window, Key.Escape);

                Assert.False(window.ViewModel.IsMoreMenuOpen);
                Assert.IsType<ControllerSetupPage>(ShownContent(window));
            });
        }

        [Fact]
        public void MoreMenu_ClickingTheScrimClosesIt()
        {
            WithWindow((_, window) =>
            {
                window.ViewModel.IsMoreMenuOpen = true;
                Pump(window);
                var scrim = Find<Border>(window, "MenuScrim");
                var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent };

                scrim.RaiseEvent(args);

                Assert.True(args.Handled);
                Assert.False(window.ViewModel.IsMoreMenuOpen);
            });
        }

        [Fact]
        public void UXN008_MenuListsItsItemsInOrderAndDisablesTheOnesWithoutAScreen()
        {
            WithWindow((_, window) =>
            {
                var items = Find<StackPanel>(window, "MoreMenu").Children.OfType<Button>().Take(4).ToList();

                Assert.Equal(
                    MenuIds,
                    items.Select(b => System.Windows.Automation.AutomationProperties.GetAutomationId(b)));
                Assert.True(Find<Button>(window, "MenuSettingsItem").IsEnabled);
                Assert.True(Find<Button>(window, "MenuCreditsItem").IsEnabled);
                Assert.False(Find<Button>(window, "MenuHelpItem").IsEnabled);
                Assert.False(Find<Button>(window, "MenuSupportItem").IsEnabled);
            });
        }

        [Fact]
        public void StyleGallery_OpensAsAPushedScreenAndShowsOnlyInDebugBuilds()
        {
            WithWindow((_, window) =>
            {
                var item = Find<Button>(window, "MenuStyleGalleryItem");
#if DEBUG
                Assert.Equal(Visibility.Visible, item.Visibility);
#else
                Assert.Equal(Visibility.Collapsed, item.Visibility);
#endif

                window.ViewModel.OpenStyleGalleryCommand.Execute(null);
                Pump(window);

                Assert.IsType<StyleGalleryPage>(ShownContent(window));
                Assert.True(window.ViewModel.IsPushedScreen);
            });
        }

        [Fact]
        public void UXA020_ReadingOrderIsSidebarMoreMenuContentThenBottomBar()
        {
            WithWindow((_, window) =>
            {
                UIElementCollection children = Find<Grid>(window, "ShellGrid").Children;
                int Index(string name) => children.IndexOf(Find<UIElement>(window, name));

                Assert.True(Index("NavSidebar") < Index("MoreButton"));
                Assert.True(Index("NavRail") < Index("MoreButton"));
                Assert.True(Index("MoreButton") < Index("MoreMenuLayer"));
                Assert.True(Index("MoreMenuLayer") < Index("ContentArea"));
                Assert.True(Index("ContentArea") < Index("NavBottomBar"));
            });
        }

        [Fact]
        public void UXN002_ConnectionsTabShowsZwiftStatus()
        {
            WithWindow((_, window) =>
            {
                window.ViewModel.SelectTab(ShellTab.Connections);
                Pump(window);

                var page = Assert.IsType<ConnectionsPage>(ShownContent(window));
                Assert.Equal("Zwift isn't connected yet", Assert.IsType<TextBlock>(page.FindName("ZwiftStatusText")).Text);
                Assert.True(window.ViewModel.Tabs[2].HasAlert);
            });
        }

        [Fact]
        public void MaximizeButton_TogglesTheWindowState()
        {
            WithWindow((_, window) =>
            {
                Click(Find<Button>(window, "MaximizeButton"));
                Assert.Equal(WindowState.Maximized, window.WindowState);
                Assert.Equal(new Thickness(8), Find<Grid>(window, "MainGrid").Margin);
                Assert.Equal("", Find<TextBlock>(window, "MaximizeIcon").Text);

                Click(Find<Button>(window, "MaximizeButton"));
                Assert.Equal(WindowState.Normal, window.WindowState);
                Assert.Equal(default, Find<Grid>(window, "MainGrid").Margin);

                Click(Find<Button>(window, "MinimizeButton"));
                Assert.Equal(WindowState.Minimized, window.WindowState);
            });
        }

        [Fact]
        public void MouseBackButton_GoesBackWhenThereIsAPreviousPage()
        {
            WithWindow((_, window) =>
            {
                window.ViewModel.OpenSettingsCommand.Execute(null);
                var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.XButton1) { RoutedEvent = Mouse.PreviewMouseDownEvent };

                window.RaiseEvent(args);

                Assert.True(args.Handled);
                Assert.IsType<ControllerSetupViewModel>(window.ViewModel.CurrentViewModel);
            });
        }

        [Fact]
        public void MouseBackButton_IsNotHandledWithoutHistory()
        {
            WithWindow((_, window) =>
            {
                var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.XButton1) { RoutedEvent = Mouse.PreviewMouseDownEvent };

                window.RaiseEvent(args);

                Assert.False(args.Handled);
                Assert.IsType<ControllerSetupViewModel>(window.ViewModel.CurrentViewModel);
            });
        }

        [Fact]
        public void OtherMouseButtons_DoNotGoBack()
        {
            WithWindow((_, window) =>
            {
                window.ViewModel.OpenSettingsCommand.Execute(null);
                var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.XButton2) { RoutedEvent = Mouse.PreviewMouseDownEvent };

                window.RaiseEvent(args);

                Assert.False(args.Handled);
                Assert.IsType<SettingsViewModel>(window.ViewModel.CurrentViewModel);
            });
        }

        [Fact]
        public void OrdinaryKeys_DoNotGoBack()
        {
            WithWindow((_, window) =>
            {
                window.ViewModel.OpenSettingsCommand.Execute(null);
                var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Left)
                {
                    RoutedEvent = Keyboard.PreviewKeyDownEvent,
                };

                window.RaiseEvent(args);

                Assert.False(args.Handled);
                Assert.IsType<SettingsViewModel>(window.ViewModel.CurrentViewModel);
            });
        }

        [Theory]
        [InlineData(Key.System, Key.Left, ModifierKeys.Alt, true)]
        [InlineData(Key.System, Key.Right, ModifierKeys.Alt, false)]
        [InlineData(Key.System, Key.Left, ModifierKeys.None, false)]
        [InlineData(Key.System, Key.Left, ModifierKeys.Alt | ModifierKeys.Shift, false)]
        [InlineData(Key.Left, Key.None, ModifierKeys.Alt, false)]
        public void IsBackKeyGesture_IsOnlyAltLeft(Key key, Key systemKey, ModifierKeys modifiers, bool expected)
        {
            Assert.Equal(expected, MainWindow.IsBackKeyGesture(key, systemKey, modifiers));
        }
    }
}
