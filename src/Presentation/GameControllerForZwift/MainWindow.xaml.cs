using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        // True while the shell moves the tree selection to match the page that is shown. Selection changes made for that
        // reason must never start a navigation, otherwise back would push the page again.
        private bool _isSyncingSelection;
        public MainWindowViewModel ViewModel { get; }
        private readonly IServiceProvider _serviceProvider;

        // One view per page view model, created on first navigation and reused afterwards.
        private readonly ViewCache _viewCache = new(new Dictionary<Type, Func<FrameworkElement>>
        {
            [typeof(ControllerSetupViewModel)] = () => new ControllerSetupPage(),
            [typeof(AboutPageViewModel)] = () => new AboutPage(),
            [typeof(SettingsViewModel)] = () => new SettingsPage(),
        });
        #endregion

        #region Constructor
        public MainWindow(MainWindowViewModel mainWindowViewModel, IServiceProvider serviceProvider)
        {
            ViewModel = mainWindowViewModel;
            _serviceProvider = serviceProvider;
            DataContext = this;

            InitializeComponent();
            Toggle_TitleButtonVisibility();

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.NavigateToStartPage();
            ShowCurrentView();
            ContentRendered += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, PrepareNextPage);

            WindowChrome.SetWindowChrome(
            this,
            new WindowChrome
            {
                CaptionHeight = 50,
                CornerRadius = default,
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = ResizeMode == ResizeMode.NoResize ? default : new Thickness(4),
                UseAeroCaptionButtons = true
            }
            );

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            this.StateChanged += MainWindow_StateChanged;
        }
        #endregion

        #region Methods

        private void Toggle_TitleButtonVisibility()
        {
            var appContextBackdropData = AppContext.GetData("Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop");
            bool disableFluentThemeWindowBackdrop = appContextBackdropData != null
                && bool.Parse(Convert.ToString(appContextBackdropData, System.Globalization.CultureInfo.InvariantCulture) ?? bool.FalseString);

            if (!disableFluentThemeWindowBackdrop)
            {
                // Only when theme resources are loaded and a theme mode is active.
                if (Application.Current.Resources.MergedDictionaries.Count > 0
                    && Application.Current.ThemeMode != ThemeMode.None)
                {
                    if (SystemParameters.HighContrast)
                    {
                        MinimizeButton.Visibility = Visibility.Visible;
                        MaximizeButton.Visibility = Visibility.Visible;
                        CloseButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MinimizeButton.Visibility = Visibility.Collapsed;
                        MaximizeButton.Visibility = Visibility.Collapsed;
                        CloseButton.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (SystemParameters.HighContrast)
                {
                    MinimizeButton.Visibility = Visibility.Visible;
                    MaximizeButton.Visibility = Visibility.Visible;
                    CloseButton.Visibility = Visibility.Visible;
                }
                else
                {
                    MinimizeButton.Visibility = Visibility.Collapsed;
                    MaximizeButton.Visibility = Visibility.Collapsed;
                    CloseButton.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            MainGrid.Margin = this.WindowState == WindowState.Maximized ? new Thickness(8) : default;
        }

        private void PagesTreeView_SelectedItemChanged()
        {
            if (_isSyncingSelection)
            {
                return;
            }

            if (PagesTreeView.SelectedItem is PagesInfoDataItem navItem)
            {
                ViewModel.NavigateToPage(navItem);
                var tvi = PagesTreeView.ItemContainerGenerator.ContainerFromItem(navItem) as TreeViewItem;
                if (tvi != null)
                {
                    tvi.BringIntoView();
                }
            }
        }

        private void PagesTreeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && PagesTreeView.SelectedItem is not null)
            {
                SelectedItemChanged(PagesTreeView.ItemContainerGenerator.ContainerFromItem(((TreeView)sender).SelectedItem) as TreeViewItem);
            }
        }

        private void PagesTreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is ToggleButton)
            {
                return;
            }
            if (PagesTreeView.SelectedItem is null)
            {
                return;
            }
            SelectedItemChanged(PagesTreeView.ItemContainerGenerator.ContainerFromItem(((TreeView)sender).SelectedItem) as TreeViewItem);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement((Button)sender);
            peer.RaiseNotificationEvent(
               AutomationNotificationKind.Other,
                AutomationNotificationProcessing.ImportantMostRecent,
                "Settings Page Opened",
                "ButtonClickedActivity"
            );
        }

        private void PagesTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            if (PagesTreeView.Items.Count > 0)
            {
                TreeViewItem firstItem = (TreeViewItem)PagesTreeView.ItemContainerGenerator.ContainerFromItem(PagesTreeView.Items[0]);
                if (firstItem != null)
                {
                    firstItem.IsSelected = true;
                }
            }
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeIcon.Text = "\uE922";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeIcon.Text = "\uE923";
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SelectedItemChanged(TreeViewItem? tvi)
        {
            PagesTreeView_SelectedItemChanged();
            if (tvi != null)
            {
                tvi.IsExpanded = !tvi.IsExpanded;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.CurrentViewModel))
            {
                ShowCurrentView();
                SyncTreeSelection(ViewModel.CurrentViewModel?.GetType());
            }
        }

        // Pages that have not been visited yet are built and laid out while the app is idle, one per idle turn, so the
        // first visit is instant. The About page is the slow one: it converts the acknowledgements markdown into a
        // FlowDocument and parses the ko-fi SVG.
        private static readonly Type[] _pagesToPrepare = { typeof(AboutPageViewModel), typeof(SettingsViewModel) };
        private int _nextPageToPrepare;

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

        private static void ClearSelection(ItemsControl parent)
        {
            foreach (object item in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem container)
                {
                    container.IsSelected = false;
                    ClearSelection(container);
                }
            }
        }

        private void ShowCurrentView()
        {
            RootContent.Content = _viewCache.GetView(ViewModel.CurrentViewModel);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (IsBackKeyGesture(e.Key, e.SystemKey, Keyboard.Modifiers) && TryGoBack())
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

        /// <summary>Selects the tree item of the page that is shown, without navigating.</summary>
        private void SyncTreeSelection(Type? viewModelType)
        {
            List<PagesInfoDataItem> list = ViewModel.GetNavigationItemHierarchyFromViewModelType(viewModelType);

            if (list.Count == 0)
            {
                // The page has no tree item (Settings): show no selection. Navigation must not start from this.
                _isSyncingSelection = true;
                try
                {
                    ClearSelection(PagesTreeView);
                }
                finally
                {
                    _isSyncingSelection = false;
                }
            }
            else
            {
                _isSyncingSelection = true;
                try
                {
                    TreeViewItem? selectedTreeViewItem = null;
                    ItemsControl itemsControl = PagesTreeView;
                    foreach (PagesInfoDataItem item in list)
                    {
                        var tvi = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                        if (tvi != null)
                        {
                            tvi.IsExpanded = true;
                            if (item != list[^1])
                            {
                                // Realize the child containers before looking for the next level.
                                tvi.UpdateLayout();
                            }
                            itemsControl = tvi;
                            selectedTreeViewItem = tvi;
                        }
                    }

                    if (selectedTreeViewItem != null)
                    {
                        selectedTreeViewItem.IsSelected = true;
                    }
                }
                finally
                {
                    _isSyncingSelection = false;
                }
            }
        }
        #endregion
    }
}
