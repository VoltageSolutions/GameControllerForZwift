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

        private void SelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
                Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () => ShowBoundSelection(comboBox));
        }

        /// <summary>
        /// Makes the ComboBox show its bound SelectedItem. When the view model refuses a pick (for example Cancel in CFG-024 or
        /// CFG-040), the binding puts the previous item back while the ComboBox is still applying the pick. The ComboBox then
        /// keeps the previous item as SelectedItem but shows the picked entry. Selecting the bound item's index again, once
        /// the pick is done, shows it.
        /// </summary>
        internal static void ShowBoundSelection(ComboBox comboBox)
        {
            ArgumentNullException.ThrowIfNull(comboBox);

            var selected = comboBox.SelectedItem;
            int index = selected is null ? -1 : comboBox.Items.IndexOf(selected);
            if (comboBox.SelectedIndex != index)
                comboBox.SetCurrentValue(Selector.SelectedIndexProperty, index);
        }

        private void MoreConfigurationActionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { ContextMenu: { } menu } button)
                return;

            // The menu's DataContext binding reads the placement target's DataContext, so the command bindings resolve.
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }
}
