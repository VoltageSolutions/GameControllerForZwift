using GameControllerForZwift.UI.WPF.Localization;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF
{
    /// <summary>
    /// The window shell. It shows the view of the current page, places the tabs for the window width, and moves focus
    /// into and out of the More menu.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }
        private readonly IServiceProvider _serviceProvider;

        // One view per page view model, created on first navigation and reused afterwards.
        private readonly ViewCache _viewCache = new(new Dictionary<Type, Func<FrameworkElement>>
        {
            [typeof(RideViewModel)] = () => new RidePage(),
            [typeof(ControllerSetupViewModel)] = () => new ControllerSetupPage(),
            [typeof(ConnectionsViewModel)] = () => new ConnectionsPage(),
            [typeof(AboutPageViewModel)] = () => new AboutPage(),
            [typeof(SettingsViewModel)] = () => new SettingsPage(),
            [typeof(StyleGalleryViewModel)] = () => new StyleGalleryPage(),
        });

        // Pages that have not been visited yet are built and laid out while the app is idle, one per idle turn, so the
        // first visit is instant. The About page is the slow one: it converts the acknowledgements markdown into a
        // FlowDocument and parses the ko-fi SVG.
        private static readonly Type[] _pagesToPrepare =
        [
            typeof(RideViewModel), typeof(ConnectionsViewModel), typeof(AboutPageViewModel), typeof(SettingsViewModel),
        ];
        private int _nextPageToPrepare;

        public MainWindow(MainWindowViewModel mainWindowViewModel, IServiceProvider serviceProvider)
        {
            ViewModel = mainWindowViewModel;
            _serviceProvider = serviceProvider;
            DataContext = this;

            InitializeComponent();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.NavigateToStartPage();
            ShowCurrentView();
            ContentRendered += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, PrepareNextPage);
            SizeChanged += (_, e) => ViewModel.UpdateLayoutForWidth(e.NewSize.Width);

            WindowChrome.SetWindowChrome(
                this,
                new WindowChrome
                {
                    CaptionHeight = 48,
                    CornerRadius = default,
                    GlassFrameThickness = new Thickness(-1),
                    ResizeBorderThickness = ResizeMode == ResizeMode.NoResize ? default : new Thickness(4),
                    // The window paints its own background, so the app draws the caption buttons.
                    UseAeroCaptionButtons = false,
                });

            StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            MainGrid.Margin = WindowState == WindowState.Maximized ? new Thickness(8) : default;
            MaximizeIcon.Text = WindowState == WindowState.Maximized ? "" : "";
            System.Windows.Automation.AutomationProperties.SetName(MaximizeButton, Localizer.Get(WindowState == WindowState.Maximized ? "CaptionRestore" : "CaptionMaximize"));
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeWindow(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void CloseWindow(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void MenuScrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.CloseMoreMenuCommand.Execute(null);
            e.Handled = true;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainWindowViewModel.CurrentViewModel):
                    ShowCurrentView();
                    break;
                case nameof(MainWindowViewModel.IsMoreMenuOpen):
                    MoveFocusForMenu(ViewModel.IsMoreMenuOpen);
                    break;
            }
        }

        // Opening the menu moves focus to its first item. Closing it returns focus to the More button when focus was in
        // the menu, so keyboard users stay where they were.
        private void MoveFocusForMenu(bool isOpen)
        {
            if (isOpen)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, () => MenuSettingsItem.Focus());
            }
            else if (MoreMenu.IsKeyboardFocusWithin || MoreButton.IsKeyboardFocused)
            {
                MoreButton.Focus();
            }
        }

        private void PrepareNextPage()
        {
            if (_nextPageToPrepare >= _pagesToPrepare.Length)
            {
                return;
            }

            object viewModel = _serviceProvider.GetRequiredService(_pagesToPrepare[_nextPageToPrepare++]);
            if (_viewCache.GetView(viewModel) is FrameworkElement view && view.Parent is null)
            {
                var size = new Size(RootContent.ActualWidth, RootContent.ActualHeight);
                view.Measure(size);
                view.Arrange(new Rect(size));
                view.UpdateLayout();
            }

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, PrepareNextPage);
        }

        private void ShowCurrentView()
        {
            RootContent.Content = _viewCache.GetView(ViewModel.CurrentViewModel);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && ViewModel.IsMoreMenuOpen)
            {
                ViewModel.CloseMoreMenuCommand.Execute(null);
                e.Handled = true;
            }
            else if (IsBackKeyGesture(e.Key, e.SystemKey, Keyboard.Modifiers) && TryGoBack())
            {
                e.Handled = true;
            }

            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// True for Alt+Left. With Alt held, WPF reports the key as <see cref="Key.System"/> and the real key as the
        /// system key.
        /// </summary>
        public static bool IsBackKeyGesture(Key key, Key systemKey, ModifierKeys modifiers)
        {
            return key == Key.System && systemKey == Key.Left && modifiers == ModifierKeys.Alt;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            // The mouse back button is Back.
            if (e.ChangedButton == MouseButton.XButton1 && TryGoBack())
            {
                e.Handled = true;
            }

            base.OnPreviewMouseDown(e);
        }

        private bool TryGoBack()
        {
            if (!ViewModel.BackCommand.CanExecute(null))
            {
                return false;
            }

            ViewModel.BackCommand.Execute(null);
            return true;
        }
    }
}
