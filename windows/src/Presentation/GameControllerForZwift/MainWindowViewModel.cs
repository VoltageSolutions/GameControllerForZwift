using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.UI.WPF.Localization;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using System.ComponentModel;

namespace GameControllerForZwift.UI.WPF
{
    /// <summary>The three tabs, in the order the spec gives them (UX-N-001).</summary>
    public enum ShellTab
    {
        Ride,
        Buttons,
        Connections,
    }

    /// <summary>Where the tabs go for the window width (UX-N-010).</summary>
    public enum NavLayout
    {
        /// <summary>Wide window: a sidebar with icons and labels.</summary>
        Sidebar,

        /// <summary>Medium window: a compact rail.</summary>
        Rail,

        /// <summary>Narrow window: a floating bar at the bottom.</summary>
        BottomBar,
    }

    /// <summary>One tab in the navigation.</summary>
    public sealed partial class NavTab : ObservableObject
    {
        public NavTab(ShellTab tab, string label, string automationId, Type viewModelType)
        {
            Tab = tab;
            Label = label;
            AutomationId = automationId;
            ViewModelType = viewModelType;
        }

        public ShellTab Tab { get; }

        public string Label { get; }

        /// <summary>The UI Automation id, as in docs/spec/ux/accessibility.md.</summary>
        public string AutomationId { get; }

        /// <summary>The view model the tab shows.</summary>
        public Type ViewModelType { get; }

        /// <summary>True when the tab shows its warning dot.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AccessibleName))]
        private bool _hasAlert;

        /// <summary>The name a screen reader reads. The dot has words too (UX-N-002).</summary>
        public string AccessibleName => HasAlert ? Localizer.Format("NavConnectionsAlert", Label) : Label;
    }

    /// <summary>
    /// View model of the window shell: the three tabs, the More button and its menu, back, and where the tabs go for
    /// the window width (docs/spec/ux/navigation.md).
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>At this width and wider the tabs are a sidebar.</summary>
        public const double SidebarMinWidth = 1000;

        /// <summary>At this width and wider, up to the sidebar width, the tabs are a compact rail.</summary>
        public const double RailMinWidth = 640;

        private readonly INavigationService _navigationService;
        private readonly ZwiftStatus _zwiftStatus;
        private readonly bool _showsStyleGallery;

        public MainWindowViewModel(INavigationService navigationService, ZwiftStatus? zwiftStatus = null)
        {
            ArgumentNullException.ThrowIfNull(navigationService);
            _navigationService = navigationService;
            _navigationService.PropertyChanged += OnNavigationServicePropertyChanged;
            _zwiftStatus = zwiftStatus ?? new ZwiftStatus();
            _zwiftStatus.PropertyChanged += OnZwiftStatusPropertyChanged;
            _showsStyleGallery = BuildInfo.IsDebug;

            Tabs =
            [
                new NavTab(ShellTab.Ride, Localizer.Get("NavRide"), "NavRideTab", typeof(RideViewModel)),
                new NavTab(ShellTab.Buttons, Localizer.Get("NavButtons"), "NavButtonsTab", typeof(ControllerSetupViewModel)),
                new NavTab(ShellTab.Connections, Localizer.Get("NavConnections"), "NavConnectionsTab", typeof(ConnectionsViewModel)),
            ];
            UpdateConnectionsAlert();
        }

        /// <summary>The tabs, in order.</summary>
        public IReadOnlyList<NavTab> Tabs { get; }

        /// <summary>The view model of the page being shown. The shell shows the matching view from its view cache.</summary>
        public object? CurrentViewModel => _navigationService.CurrentViewModel;

        /// <summary>
        /// The tab of the page being shown, or null on a pushed screen (UX-N-009). Setting it shows that tab and
        /// clears the history.
        /// </summary>
        public NavTab? SelectedTab
        {
            get => Tabs.FirstOrDefault(t => t.ViewModelType == CurrentViewModel?.GetType());
            set
            {
                if (value is not null)
                {
                    IsMoreMenuOpen = false;
                    _navigationService.NavigateToRoot(value.ViewModelType);
                }
            }
        }

        /// <summary>True on a screen opened from the More menu. It shows a back button (UX-N-016).</summary>
        public bool IsPushedScreen => CurrentViewModel is not null && SelectedTab is null;

        /// <summary>True when there is a previous page to return to.</summary>
        public bool CanGoBack => _navigationService.CanGoBack;

        /// <summary>True while Zwift is connected.</summary>
        public bool IsZwiftConnected => _zwiftStatus.IsConnected;

        /// <summary>Where the tabs go. The window sets it from its width.</summary>
        [ObservableProperty]
        private NavLayout _navLayout = NavLayout.Sidebar;

        /// <summary>True while the More menu is open.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MoreButtonName))]
        private bool _isMoreMenuOpen;

        /// <summary>The More button's name for screen readers.</summary>
        public string MoreButtonName => Localizer.Get(IsMoreMenuOpen ? "MenuClose" : "MenuMore");

        /// <summary>Picks the tab placement for a window width (UX-N-010, UX-N-011).</summary>
        public static NavLayout LayoutForWidth(double width) => width switch
        {
            >= SidebarMinWidth => NavLayout.Sidebar,
            >= RailMinWidth => NavLayout.Rail,
            _ => NavLayout.BottomBar,
        };

        /// <summary>Updates <see cref="NavLayout"/> for a new window width.</summary>
        public void UpdateLayoutForWidth(double width) => NavLayout = LayoutForWidth(width);

        /// <summary>Shows the first tab after start. The history starts empty.</summary>
        public void NavigateToStartPage() => SelectTab(ShellTab.Buttons);

        /// <summary>Shows a tab and clears the history.</summary>
        public void SelectTab(ShellTab tab) => SelectedTab = Tabs.Single(t => t.Tab == tab);

        [RelayCommand]
        private void ToggleMoreMenu() => IsMoreMenuOpen = !IsMoreMenuOpen;

        [RelayCommand]
        private void CloseMoreMenu() => IsMoreMenuOpen = false;

        /// <summary>Opens Settings as a pushed screen (UX-N-009).</summary>
        [RelayCommand]
        private void OpenSettings()
        {
            IsMoreMenuOpen = false;
            _navigationService.NavigateTo<SettingsViewModel>();
        }

        /// <summary>Opens Credits as a pushed screen (UX-N-009).</summary>
        [RelayCommand]
        private void OpenCredits()
        {
            IsMoreMenuOpen = false;
            _navigationService.NavigateTo<AboutPageViewModel>();
        }

        /// <summary>
        /// True in Debug builds, where the More menu also lists the style gallery (docs/spec/ux/navigation.md). Release
        /// builds never show it.
        /// </summary>
        public bool ShowsStyleGallery => _showsStyleGallery;

        /// <summary>Opens the style gallery as a pushed screen. Only Debug builds can.</summary>
        [RelayCommand(CanExecute = nameof(ShowsStyleGallery))]
        private void OpenStyleGallery()
        {
            IsMoreMenuOpen = false;
            _navigationService.NavigateTo<StyleGalleryViewModel>();
        }

        /// <summary>Help and feedback has no screen, so its menu item is disabled.</summary>
        [RelayCommand(CanExecute = nameof(HasHelpScreen))]
        private void OpenHelp()
        {
        }

        /// <summary>Support this project has no screen, so its menu item is disabled.</summary>
        [RelayCommand(CanExecute = nameof(HasSupportScreen))]
        private void OpenSupport()
        {
        }

        private static bool HasHelpScreen() => false;

        private static bool HasSupportScreen() => false;

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void Back()
        {
            IsMoreMenuOpen = false;
            _navigationService.GoBack();
        }

        private void OnNavigationServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(INavigationService.CurrentViewModel):
                    OnPropertyChanged(nameof(CurrentViewModel));
                    OnPropertyChanged(nameof(SelectedTab));
                    OnPropertyChanged(nameof(IsPushedScreen));
                    break;
                case nameof(INavigationService.CanGoBack):
                    OnPropertyChanged(nameof(CanGoBack));
                    BackCommand.NotifyCanExecuteChanged();
                    break;
            }
        }

        private void OnZwiftStatusPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ZwiftStatus.IsConnected))
            {
                OnPropertyChanged(nameof(IsZwiftConnected));
                UpdateConnectionsAlert();
            }
        }

        // UX-N-002: the Connections tab shows its dot whenever Zwift is not connected.
        private void UpdateConnectionsAlert() =>
            Tabs.Single(t => t.Tab == ShellTab.Connections).HasAlert = !_zwiftStatus.IsConnected;
    }
}
