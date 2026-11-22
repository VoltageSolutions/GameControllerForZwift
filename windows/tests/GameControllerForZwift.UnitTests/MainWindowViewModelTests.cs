using GameControllerForZwift.Core;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;
using System.ComponentModel;

namespace GameControllerForZwift.UI.WPF.Tests
{
    public class MainWindowViewModelTests
    {
        private readonly INavigationService _navigation = Substitute.For<INavigationService>();
        private readonly IZwiftConnection _zwift = Substitute.For<IZwiftConnection>();
        private readonly MainWindowViewModel _viewModel;

        public MainWindowViewModelTests()
        {
            _viewModel = new MainWindowViewModel(_navigation, new ZwiftStatus(_zwift));
        }

        private void RaiseNavigation(string propertyName) =>
            _navigation.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(_navigation, new PropertyChangedEventArgs(propertyName));

        private void Show(object viewModel)
        {
            _navigation.CurrentViewModel.Returns(viewModel);
            RaiseNavigation(nameof(INavigationService.CurrentViewModel));
        }

        private static readonly string[] TabLabels = ["Ride", "Buttons", "Connections"];
        private static readonly string[] TabIds = ["NavRideTab", "NavButtonsTab", "NavConnectionsTab"];

        [Fact]
        public void Constructor_RejectsANullNavigationService()
        {
            Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(null!));
        }

        [Fact]
        public void UXN001_TabsAreRideButtonsAndConnectionsInOrder()
        {
            Assert.Equal(TabLabels, _viewModel.Tabs.Select(t => t.Label));
            Assert.Equal(TabIds, _viewModel.Tabs.Select(t => t.AutomationId));
            Assert.Equal(typeof(RideViewModel), _viewModel.Tabs[0].ViewModelType);
            Assert.Equal(typeof(ControllerSetupViewModel), _viewModel.Tabs[1].ViewModelType);
            Assert.Equal(typeof(ConnectionsViewModel), _viewModel.Tabs[2].ViewModelType);
        }

        [Fact]
        public void NavigateToStartPage_ShowsTheButtonsTabWithAnEmptyHistory()
        {
            _viewModel.NavigateToStartPage();

            _navigation.Received(1).NavigateToRoot(typeof(ControllerSetupViewModel));
        }

        [Theory]
        [InlineData(ShellTab.Ride, typeof(RideViewModel))]
        [InlineData(ShellTab.Buttons, typeof(ControllerSetupViewModel))]
        [InlineData(ShellTab.Connections, typeof(ConnectionsViewModel))]
        public void SelectTab_NavigatesToItsRootAndClosesTheMenu(ShellTab tab, Type viewModelType)
        {
            _viewModel.IsMoreMenuOpen = true;

            _viewModel.SelectTab(tab);

            _navigation.Received(1).NavigateToRoot(viewModelType);
            Assert.False(_viewModel.IsMoreMenuOpen);
        }

        [Fact]
        public void SelectedTab_SetToNull_DoesNothing()
        {
            _viewModel.SelectedTab = null;

            _navigation.DidNotReceiveWithAnyArgs().NavigateToRoot(default!);
        }

        [Fact]
        public void SelectedTab_FollowsTheShownPage()
        {
            var changed = new List<string?>();
            _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            using var context = new GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext();
            using var status = new ZwiftStatus();
            var setup = context.CreateViewModel();
            using var ride = new RideViewModel(status, setup, context.Settings, context.Dialogs, _navigation,
                new ConnectionsViewModel(status, setup, _ => { }));
            Show(ride);

            Assert.Equal(ShellTab.Ride, _viewModel.SelectedTab!.Tab);
            Assert.False(_viewModel.IsPushedScreen);
            Assert.Contains(nameof(MainWindowViewModel.CurrentViewModel), changed);
            Assert.Contains(nameof(MainWindowViewModel.SelectedTab), changed);
            Assert.Contains(nameof(MainWindowViewModel.IsPushedScreen), changed);
        }

        [Fact]
        public void UXN009_APushedScreenSelectsNoTabAndShowsBack()
        {
            Show(new object());

            Assert.Null(_viewModel.SelectedTab);
            Assert.True(_viewModel.IsPushedScreen);
        }

        [Fact]
        public void IsPushedScreen_IsFalseBeforeTheFirstPage()
        {
            Assert.Null(_viewModel.CurrentViewModel);
            Assert.False(_viewModel.IsPushedScreen);
        }

        [Fact]
        public void UXN008_UXN009_SettingsAndCreditsArePushedAndCloseTheMenu()
        {
            _viewModel.IsMoreMenuOpen = true;
            _viewModel.OpenSettingsCommand.Execute(null);
            _navigation.Received(1).NavigateTo<SettingsViewModel>();
            Assert.False(_viewModel.IsMoreMenuOpen);

            _viewModel.IsMoreMenuOpen = true;
            _viewModel.OpenCreditsCommand.Execute(null);
            _navigation.Received(1).NavigateTo<CreditsViewModel>();
            Assert.False(_viewModel.IsMoreMenuOpen);
        }

        [Fact]
        public void StyleGallery_IsOnlyOfferedInDebugBuilds()
        {
#if DEBUG
            Assert.True(_viewModel.ShowsStyleGallery);
            Assert.True(_viewModel.OpenStyleGalleryCommand.CanExecute(null));
#else
            Assert.False(_viewModel.ShowsStyleGallery);
            Assert.False(_viewModel.OpenStyleGalleryCommand.CanExecute(null));
#endif
            _viewModel.IsMoreMenuOpen = true;

            _viewModel.OpenStyleGalleryCommand.Execute(null);

            _navigation.Received(1).NavigateTo<StyleGalleryViewModel>();
            Assert.False(_viewModel.IsMoreMenuOpen);
        }

        [Fact]
        public void Help_IsDisabledBecauseItHasNoScreen()
        {
            Assert.False(_viewModel.OpenHelpCommand.CanExecute(null));
            _viewModel.OpenHelpCommand.Execute(null);
            _navigation.DidNotReceiveWithAnyArgs().NavigateTo(default(Type)!);
        }

        [Fact]
        public void UXN008_UXN009_SupportIsPushedAndClosesTheMenu()
        {
            _viewModel.IsMoreMenuOpen = true;

            _viewModel.OpenSupportCommand.Execute(null);

            _navigation.Received(1).NavigateTo<SupportViewModel>();
            Assert.False(_viewModel.IsMoreMenuOpen);
        }

        [Fact]
        public void MoreMenu_TogglesAndCloses()
        {
            var changed = new List<string?>();
            _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            Assert.Equal("More: settings, help, credits, support", _viewModel.MoreButtonName);

            _viewModel.ToggleMoreMenuCommand.Execute(null);
            Assert.True(_viewModel.IsMoreMenuOpen);
            Assert.Equal("Close menu", _viewModel.MoreButtonName);
            Assert.Contains(nameof(MainWindowViewModel.MoreButtonName), changed);

            _viewModel.ToggleMoreMenuCommand.Execute(null);
            Assert.False(_viewModel.IsMoreMenuOpen);

            _viewModel.IsMoreMenuOpen = true;
            _viewModel.CloseMoreMenuCommand.Execute(null);
            Assert.False(_viewModel.IsMoreMenuOpen);
        }

        [Fact]
        public void Back_GoesBackAndClosesTheMenu()
        {
            _navigation.CanGoBack.Returns(true);
            RaiseNavigation(nameof(INavigationService.CanGoBack));
            _viewModel.IsMoreMenuOpen = true;

            _viewModel.BackCommand.Execute(null);

            _navigation.Received(1).GoBack();
            Assert.False(_viewModel.IsMoreMenuOpen);
        }

        [Fact]
        public void BackCommand_FollowsCanGoBack()
        {
            bool raised = false;
            _viewModel.BackCommand.CanExecuteChanged += (_, _) => raised = true;
            Assert.False(_viewModel.BackCommand.CanExecute(null));

            _navigation.CanGoBack.Returns(true);
            RaiseNavigation(nameof(INavigationService.CanGoBack));

            Assert.True(raised);
            Assert.True(_viewModel.CanGoBack);
            Assert.True(_viewModel.BackCommand.CanExecute(null));
        }

        [Fact]
        public void OtherNavigationChanges_AreIgnored()
        {
            var changed = new List<string?>();
            _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            RaiseNavigation("Something else");

            Assert.Empty(changed);
        }

        [Theory]
        [InlineData(1600, NavLayout.Sidebar)]
        [InlineData(1000, NavLayout.Sidebar)]
        [InlineData(999.5, NavLayout.Rail)]
        [InlineData(640, NavLayout.Rail)]
        [InlineData(639, NavLayout.BottomBar)]
        [InlineData(400, NavLayout.BottomBar)]
        public void UXN010_LayoutForWidth_PlacesTheTabs(double width, NavLayout expected)
        {
            Assert.Equal(expected, MainWindowViewModel.LayoutForWidth(width));

            _viewModel.UpdateLayoutForWidth(width);
            Assert.Equal(expected, _viewModel.NavLayout);
        }

        [Fact]
        public void UXN002_ConnectionsTabShowsItsDotWhileZwiftIsNotConnected()
        {
            NavTab connections = _viewModel.Tabs[2];
            Assert.False(_viewModel.IsZwiftConnected);
            Assert.True(connections.HasAlert);
            Assert.Equal("Connections, Zwift not connected", connections.AccessibleName);
            Assert.False(_viewModel.Tabs[0].HasAlert);
            Assert.Equal("Ride", _viewModel.Tabs[0].AccessibleName);

            var changed = new List<string?>();
            _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            _zwift.ConnectionChanged += Raise.Event<EventHandler<bool>>(_zwift, true);

            Assert.True(_viewModel.IsZwiftConnected);
            Assert.False(connections.HasAlert);
            Assert.Equal("Connections", connections.AccessibleName);
            Assert.Contains(nameof(MainWindowViewModel.IsZwiftConnected), changed);
        }

        [Fact]
        public void WithoutAZwiftStatus_ZwiftShowsAsNotConnected()
        {
            var viewModel = new MainWindowViewModel(_navigation);

            Assert.False(viewModel.IsZwiftConnected);
            Assert.True(viewModel.Tabs[2].HasAlert);
        }
    }
}
