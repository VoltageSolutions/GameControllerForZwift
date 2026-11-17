using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.UI.WPF.Tests.ViewModels;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.Views
{
    /// <summary>
    /// The pages' code-behind: the import notice timer (CFG-031) and the configuration actions menu.
    /// </summary>
    [Collection(GameControllerForZwift.UnitTests.WpfWindows.Name)]
    public sealed class PageTests : IDisposable
    {
        private readonly ConfigurationTestContext _context = new();

        public void Dispose() => _context.Dispose();

        private static void RunTick(ControllerSetupPage page) =>
            typeof(ControllerSetupPage).GetMethod("NoticeTimer_Tick", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(page, [null, EventArgs.Empty]);

        private static System.Windows.Threading.DispatcherTimer Timer(ControllerSetupPage page) =>
            (System.Windows.Threading.DispatcherTimer)typeof(ControllerSetupPage)
                .GetField("_noticeTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;

        private ControllerSetupPage ImportInto(out ControllerSetupViewModel viewModel)
        {
            viewModel = _context.CreateViewModel(Xbox);
            var page = new ControllerSetupPage { DataContext = viewModel };
            var source = new SavedConfiguration(Guid.NewGuid(), "Shared", Xbox,
                ConfigurationMappings.Normalize([new(ControllerInput.Y, ZwiftFunction.RideOn)]), DateTimeOffset.UtcNow);
            _context.Dialogs.PickedFiles.Enqueue(_context.WriteExternalFile(source));
            viewModel.ImportCommand.Execute(null);
            return page;
        }

        [Fact]
        public void CFG031_NoticeShown_StartsTimer_AndTickDismissesIt()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var page = ImportInto(out var viewModel);

                Assert.NotNull(viewModel.NoticeText);
                Assert.True(Timer(page).IsEnabled);

                RunTick(page);

                Assert.Null(viewModel.NoticeText);
                Assert.False(Timer(page).IsEnabled);
            });
        }

        [Fact]
        public void CFG031_NoticeDismissed_StopsTimer()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var page = ImportInto(out var viewModel);

                viewModel.DismissNoticeCommand.Execute(null);

                Assert.False(Timer(page).IsEnabled);
            });
        }

        [Fact]
        public void ControllerSetupPage_OtherPropertyChange_LeavesTimerStopped()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var viewModel = _context.CreateViewModel(Xbox);
                var page = new ControllerSetupPage { DataContext = viewModel };

                viewModel.Selector(ControllerInput.Y).SelectedZwiftFunction = ZwiftFunction.RideOn;

                Assert.False(Timer(page).IsEnabled);
            });
        }

        [Fact]
        public void ControllerSetupPage_MoreActionsClick_OpensMenuWithPageDataContext()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var page = new ControllerSetupPage { DataContext = _context.CreateViewModel(Xbox) };
                var window = new Window { Content = page, ShowInTaskbar = false, Width = 400, Height = 300 };
                window.Show();
                try
                {
                    var button = (Button)page.FindName("MoreConfigurationActionsButton");
                    var menu = button.ContextMenu!;

                    typeof(ControllerSetupPage).GetMethod("MoreConfigurationActionsButton_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
                        .Invoke(page, [button, new RoutedEventArgs()]);

                    Assert.True(menu.IsOpen);
                    Assert.Same(page.DataContext, menu.DataContext);
                    menu.IsOpen = false;
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ControllerSetupPage_MoreActionsClickFromOtherSender_Ignored()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var page = new ControllerSetupPage { DataContext = _context.CreateViewModel(Xbox) };

                typeof(ControllerSetupPage).GetMethod("MoreConfigurationActionsButton_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(page, [new object(), new RoutedEventArgs()]);
            });
        }

        [Fact]
        public void SettingsPage_ViewModel_IsTheDataContext()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var viewModel = new GameControllerForZwift.UI.WPF.ViewModels.SettingsViewModel(_context.Store, _context.Settings, _context.Dialogs);

                var page = new SettingsPage { DataContext = viewModel };

                Assert.Same(viewModel, page.ViewModel);
                Assert.Same(viewModel, page.DataContext);
            });
        }
    }
}
