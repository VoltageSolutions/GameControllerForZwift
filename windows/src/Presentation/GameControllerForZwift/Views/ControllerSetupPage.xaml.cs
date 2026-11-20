using GameControllerForZwift.UI.WPF.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>
    /// The Buttons screen (docs/spec/screens/buttons.md).
    /// </summary>
    public partial class ControllerSetupPage : UserControl
    {
        #region Fields

        /// <summary>The narrowest a row is before the list drops to fewer columns.</summary>
        internal const double MinimumRowWidth = 320;

        /// <summary>The widest the cards on Sticks and triggers grow, together, before the page leaves the rest empty.</summary>
        internal const double MaximumCardsWidth = 1000;

        /// <summary>The widest the mapping picker grows.</summary>
        internal const double MaximumPickerWidth = 420;

        // The space a row leaves to its neighbor (its right margin), and what a card takes on each side.
        private const double RowGap = 12;
        private const double CardPadding = 24;

        /// <summary>The width of one row in a list, so rows fill the page in as many columns as fit.</summary>
        public static readonly DependencyProperty RowWidthProperty =
            DependencyProperty.Register(nameof(RowWidth), typeof(double), typeof(ControllerSetupPage), new PropertyMetadata(MinimumRowWidth + RowGap));

        /// <summary>The width of one card on Sticks and triggers, so cards fill the page in as many columns as fit.</summary>
        public static readonly DependencyProperty CardWidthProperty =
            DependencyProperty.Register(nameof(CardWidth), typeof(double), typeof(ControllerSetupPage), new PropertyMetadata(MinimumRowWidth + 2 * CardPadding + RowGap));

        /// <summary>The width of one row in a list inside a card.</summary>
        public static readonly DependencyProperty CardRowWidthProperty =
            DependencyProperty.Register(nameof(CardRowWidth), typeof(double), typeof(ControllerSetupPage), new PropertyMetadata(MinimumRowWidth + RowGap));

        /// <summary>The view model shown by this view. The view cache assigns it as the DataContext.</summary>
        public ControllerSetupViewModel ViewModel => (ControllerSetupViewModel)DataContext;

        private static readonly TimeSpan NoticeDuration = TimeSpan.FromSeconds(6);
        private readonly DispatcherTimer _noticeTimer;
        private ControllerSetupViewModel? _subscribedViewModel;
        private IInputElement? _focusBeforePanel;

        #endregion

        #region Constructor

        public ControllerSetupPage()
        {
            InitializeComponent();

            // The import notice is non-blocking and hides itself (CFG-031).
            _noticeTimer = new DispatcherTimer { Interval = NoticeDuration };
            _noticeTimer.Tick += NoticeTimer_Tick;
            DataContextChanged += OnDataContextChanged;
            SizeChanged += (_, e) => UpdateLayoutWidths(e.NewSize.Width);

            // Hiding the screen closes the function panel (SCR-BTN-005).
            Loaded += (_, _) => SetScreenActive(true);
            Unloaded += (_, _) => SetScreenActive(false);
        }
        #endregion

        /// <summary>Gets or sets the width of one row in a list.</summary>
        public double RowWidth
        {
            get => (double)GetValue(RowWidthProperty);
            set => SetValue(RowWidthProperty, value);
        }

        /// <summary>Gets or sets the width of one card on Sticks and triggers.</summary>
        public double CardWidth
        {
            get => (double)GetValue(CardWidthProperty);
            set => SetValue(CardWidthProperty, value);
        }

        /// <summary>Gets or sets the width of one row in a list inside a card.</summary>
        public double CardRowWidth
        {
            get => (double)GetValue(CardRowWidthProperty);
            set => SetValue(CardRowWidthProperty, value);
        }

        /// <summary>
        /// Returns the width of one column when a list of rows fills <paramref name="available"/>. Rows take as many columns as
        /// fit at the minimum width, then share the space evenly, so text wraps inside a row and never clips (UX-V-044).
        /// </summary>
        internal static double ColumnWidth(double available) => ColumnWidth(available, MinimumRowWidth);

        /// <summary>Returns the width of one column when items at least <paramref name="minimum"/> wide fill <paramref name="available"/>.</summary>
        internal static double ColumnWidth(double available, double minimum)
        {
            int columns = Math.Max(1, (int)Math.Floor(available / (minimum + RowGap)));
            return Math.Max(minimum, available / columns);
        }

        private void UpdateLayoutWidths(double pageWidth)
        {
            double available = Math.Max(0, pageWidth - SystemParameters.VerticalScrollBarWidth);
            RowWidth = ColumnWidth(available);
            CardWidth = ColumnWidth(Math.Min(available, MaximumCardsWidth), MinimumRowWidth + 2 * CardPadding);
            CardRowWidth = ColumnWidth(Math.Max(0, CardWidth - RowGap - 2 * CardPadding));
            ConfigurationComboBox.Width = Math.Clamp(available - RowGap, 240, MaximumPickerWidth);
        }

        private void SetScreenActive(bool active)
        {
            if (DataContext is ControllerSetupViewModel viewModel)
                viewModel.IsScreenActive = active;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_subscribedViewModel is not null)
            {
                _subscribedViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _subscribedViewModel.FunctionPicker.PropertyChanged -= FunctionPicker_PropertyChanged;
            }

            _subscribedViewModel = e.NewValue as ControllerSetupViewModel;
            if (_subscribedViewModel is not null)
            {
                _subscribedViewModel.PropertyChanged += ViewModel_PropertyChanged;
                _subscribedViewModel.FunctionPicker.PropertyChanged += FunctionPicker_PropertyChanged;
                _subscribedViewModel.IsScreenActive = IsLoaded;
            }
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

        #region Function panel

        // Focus moves into the panel when it opens and returns to what had it when it closes (SCR-BTN-037, UX-C-015).
        private void FunctionPicker_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FunctionPickerViewModel.IsOpen))
                return;

            if (ViewModel.FunctionPicker.IsOpen)
            {
                _focusBeforePanel = Keyboard.FocusedElement;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, () => FunctionPanel.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
            }
            else if (_focusBeforePanel is UIElement { IsVisible: true } previous)
            {
                Keyboard.Focus(previous);
                _focusBeforePanel = null;
            }
        }

        private void FunctionPanel_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            ViewModel.FunctionPicker.Close();
            e.Handled = true;
        }

        private void FunctionScrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.FunctionPicker.Close();
            e.Handled = true;
        }

        // Checking a radio applies the function. The view model's own selection checks the radio too, which applies the
        // function it already has, so that case changes nothing.
        private void FunctionItem_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton { DataContext: FunctionItemViewModel item })
                ViewModel.FunctionPicker.Choose(item);
        }

        #endregion

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
