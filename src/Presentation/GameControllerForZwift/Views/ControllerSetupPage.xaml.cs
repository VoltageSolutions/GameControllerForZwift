using GameControllerForZwift.UI.WPF.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>
    /// Interaction logic for ControllerSetupPage.xaml
    /// </summary>
    public partial class ControllerSetupPage : UserControl
    {
        #region Fields

        /// <summary>The view model shown by this view. The view cache assigns it as the DataContext.</summary>
        public ControllerSetupViewModel ViewModel => (ControllerSetupViewModel)DataContext;

        private static readonly TimeSpan NoticeDuration = TimeSpan.FromSeconds(6);
        private readonly DispatcherTimer _noticeTimer;
        private ControllerSetupViewModel? _subscribedViewModel;

        #endregion

        #region Constructor

        public ControllerSetupPage()
        {
            InitializeComponent();

            // The import notice is non-blocking and hides itself (CFG-031).
            _noticeTimer = new DispatcherTimer { Interval = NoticeDuration };
            _noticeTimer.Tick += NoticeTimer_Tick;
            DataContextChanged += OnDataContextChanged;
        }
        #endregion

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_subscribedViewModel is not null)
                _subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;

            _subscribedViewModel = e.NewValue as ControllerSetupViewModel;
            if (_subscribedViewModel is not null)
                _subscribedViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ControllerSetupViewModel.NoticeText))
                return;

            _noticeTimer.Stop();
            if (ViewModel.NoticeText is not null)
                _noticeTimer.Start();
        }

        private void NoticeTimer_Tick(object? sender, EventArgs e)
        {
            _noticeTimer.Stop();
            ViewModel.DismissNoticeCommand.Execute(null);
        }

        private void MoreConfigurationActionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { ContextMenu: { } menu } button)
                return;

            // A context menu is not in the page's visual tree, so give it the page's data context for the command bindings.
            menu.DataContext = DataContext;
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }
}
