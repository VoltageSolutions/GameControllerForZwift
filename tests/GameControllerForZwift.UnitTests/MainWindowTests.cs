using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF;
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
    [Collection(WpfWindows.Name)]
    public class MainWindowTests
    {
        private static ServiceProvider CreateProvider()
        {
            var dataIntegrator = Substitute.For<IDataIntegrator>();
            dataIntegrator.GetControllers().Returns(new List<IController>());
            var profileService = Substitute.For<IControllerProfileService>();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(dataIntegrator);
            services.AddSingleton(profileService);
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
            window.Show();
            Pump(window);
            return window;
        }

        // Runs everything queued so far, including the idle work that prepares the other pages.
        private static void Pump(MainWindow window)
        {
            for (int i = 0; i < 6; i++)
            {
                window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
            }
        }

        private static T Find<T>(MainWindow window, string name) where T : class => (T)window.FindName(name);

        private static TreeViewItem TreeItem(MainWindow window, Type viewModelType)
        {
            PagesInfoDataItem item = window.ViewModel.Pages.Single(p => p.ViewModelType == viewModelType);
            return (TreeViewItem)Find<TreeView>(window, "PagesTreeView").ItemContainerGenerator.ContainerFromItem(item);
        }

        private static object? ShownContent(MainWindow window) => Find<ContentControl>(window, "RootContent").Content;

        [Theory]
        [InlineData(true, true, true, false, null)]
        [InlineData(false, false, true, false, null)]
        [InlineData(false, true, false, false, null)]
        [InlineData(false, true, true, false, Visibility.Collapsed)]
        [InlineData(false, true, true, true, Visibility.Visible)]
        public void GetCaptionButtonVisibility_DependsOnThemeStateAndContrast(
            bool backdropDisabled, bool hasThemeResources, bool themeModeSet, bool highContrast, Visibility? expected)
            => Assert.Equal(expected, MainWindow.GetCaptionButtonVisibility(backdropDisabled, hasThemeResources, themeModeSet, highContrast));

        [Fact]
        public void Constructor_ShowsTheControllerSetupPageWithItsViewModel()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    var page = Assert.IsType<ControllerSetupPage>(ShownContent(window));
                    Assert.Same(provider.GetRequiredService<ControllerSetupViewModel>(), page.DataContext);
                    Assert.Same(page.DataContext, page.ViewModel);
                    Assert.False(window.ViewModel.CanGoBack);
                    Assert.True(TreeItem(window, typeof(ControllerSetupViewModel)).IsSelected);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void NavigatingToAnotherPage_ShowsItsViewAndSelectsItsTreeItem()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    PagesInfoDataItem about = window.ViewModel.Pages.Single(p => p.ViewModelType == typeof(AboutPageViewModel));

                    window.ViewModel.NavigateToPage(about);

                    var page = Assert.IsType<AboutPage>(ShownContent(window));
                    Assert.Same(provider.GetRequiredService<AboutPageViewModel>(), page.ViewModel);
                    Assert.True(TreeItem(window, typeof(AboutPageViewModel)).IsSelected);
                    Assert.False(TreeItem(window, typeof(ControllerSetupViewModel)).IsSelected);
                    Assert.True(window.ViewModel.CanGoBack);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Back_ReturnsToThePreviousPageAndAddsNoHistoryEntry()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    window.ViewModel.NavigateToPage(window.ViewModel.Pages.Single(p => p.ViewModelType == typeof(AboutPageViewModel)));

                    window.ViewModel.BackCommand.Execute(null);

                    Assert.IsType<ControllerSetupPage>(ShownContent(window));
                    Assert.True(TreeItem(window, typeof(ControllerSetupViewModel)).IsSelected);
                    Assert.False(window.ViewModel.CanGoBack);
                    Assert.False(window.ViewModel.BackCommand.CanExecute(null));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ReturningToAPage_ShowsTheSameViewInstance()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    object? first = ShownContent(window);
                    window.ViewModel.SettingsCommand.Execute(null);

                    window.ViewModel.BackCommand.Execute(null);

                    Assert.Same(first, ShownContent(window));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Settings_ShowsTheSettingsPageWithNoTreeSelection()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    window.ViewModel.SettingsCommand.Execute(null);

                    Assert.IsType<SettingsPage>(ShownContent(window));
                    Assert.True(window.ViewModel.IsSettingsShown);
                    Assert.False(TreeItem(window, typeof(ControllerSetupViewModel)).IsSelected);
                    Assert.False(TreeItem(window, typeof(AboutPageViewModel)).IsSelected);
                    Assert.True(window.ViewModel.CanGoBack);

                    window.ViewModel.BackCommand.Execute(null);

                    Assert.IsType<ControllerSetupPage>(ShownContent(window));
                    Assert.False(window.ViewModel.IsSettingsShown);
                    Assert.True(TreeItem(window, typeof(ControllerSetupViewModel)).IsSelected);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void IdleWork_BuildsThePagesThatHaveNotBeenVisited()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    var viewCache = (ViewCache)typeof(MainWindow)
                        .GetField("_viewCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                        .GetValue(window)!;
                    var views = (System.Collections.IDictionary)typeof(ViewCache)
                        .GetField("_views", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                        .GetValue(viewCache)!;

                    Assert.Equal(3, views.Count);
                    Assert.True(((FrameworkElement)views[typeof(AboutPageViewModel)]!).IsMeasureValid);
                    Assert.Null(((FrameworkElement)views[typeof(SettingsViewModel)]!).Parent);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void EnterOnTheSelectedTreeItem_NavigatesToItsPage()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    var tree = Find<TreeView>(window, "PagesTreeView");
                    TreeItem(window, typeof(AboutPageViewModel)).IsSelected = true;

                    tree.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent,
                    });

                    Assert.IsType<AboutPageViewModel>(window.ViewModel.CurrentViewModel);
                    Assert.IsType<AboutPage>(ShownContent(window));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void EnterWithoutASelection_DoesNothing()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    window.ViewModel.SettingsCommand.Execute(null);
                    var tree = Find<TreeView>(window, "PagesTreeView");

                    tree.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent,
                    });

                    Assert.IsType<SettingsViewModel>(window.ViewModel.CurrentViewModel);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ClickingASelectedTreeItem_NavigatesToItsPage()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    var tree = Find<TreeView>(window, "PagesTreeView");
                    TreeItem(window, typeof(AboutPageViewModel)).IsSelected = true;

                    tree.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent,
                        Source = tree,
                    });

                    Assert.IsType<AboutPageViewModel>(window.ViewModel.CurrentViewModel);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ClickingTheTreeWithoutASelection_DoesNothing()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    window.ViewModel.SettingsCommand.Execute(null);
                    var tree = Find<TreeView>(window, "PagesTreeView");

                    tree.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent,
                        Source = tree,
                    });

                    Assert.IsType<SettingsViewModel>(window.ViewModel.CurrentViewModel);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MouseBackButton_GoesBackWhenThereIsAPreviousPage()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    window.ViewModel.SettingsCommand.Execute(null);
                    var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.XButton1)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent,
                    };

                    window.RaiseEvent(args);

                    Assert.True(args.Handled);
                    Assert.IsType<ControllerSetupViewModel>(window.ViewModel.CurrentViewModel);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void MouseBackButton_IsNotHandledWithoutHistory()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.XButton1)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent,
                    };

                    window.RaiseEvent(args);

                    Assert.False(args.Handled);
                    Assert.IsType<ControllerSetupViewModel>(window.ViewModel.CurrentViewModel);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void OtherMouseButtons_DoNotGoBack()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    window.ViewModel.SettingsCommand.Execute(null);
                    var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent,
                    };

                    window.RaiseEvent(args);

                    Assert.False(args.Handled);
                    Assert.IsType<SettingsViewModel>(window.ViewModel.CurrentViewModel);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void OrdinaryKeys_DoNotGoBack()
        {
            TestApplication.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                MainWindow window = ShowWindow(provider);
                try
                {
                    window.ViewModel.SettingsCommand.Execute(null);
                    var args = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Left)
                    {
                        RoutedEvent = Keyboard.PreviewKeyDownEvent,
                    };

                    window.RaiseEvent(args);

                    Assert.False(args.Handled);
                    Assert.IsType<SettingsViewModel>(window.ViewModel.CurrentViewModel);
                }
                finally
                {
                    window.Close();
                }
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
