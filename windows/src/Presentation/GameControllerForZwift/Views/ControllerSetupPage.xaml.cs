using GameControllerForZwift.Core;
using GameControllerForZwift.UI.WPF.Controls;
using GameControllerForZwift.UI.WPF.Theme;
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

        internal const double MinimumRowWidth = 320;
        internal const double MaximumCardsWidth = 1000;
        internal const double MaximumPickerWidth = 420;
        private const double TwoColumnBreakpointWidth = 900;
        private const double LandscapeControllerColumnWidth = 376;

        private const double RowGap = 12;
        private const double CardPadding = 24;

        public static readonly DependencyProperty RowWidthProperty =
            DependencyProperty.Register(nameof(RowWidth), typeof(double), typeof(ControllerSetupPage), new PropertyMetadata(MinimumRowWidth + RowGap));

        public static readonly DependencyProperty CardWidthProperty =
            DependencyProperty.Register(nameof(CardWidth), typeof(double), typeof(ControllerSetupPage), new PropertyMetadata(MinimumRowWidth + 2 * CardPadding + RowGap));

        public static readonly DependencyProperty CardRowWidthProperty =
            DependencyProperty.Register(nameof(CardRowWidth), typeof(double), typeof(ControllerSetupPage), new PropertyMetadata(MinimumRowWidth + RowGap));

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

            _noticeTimer = new DispatcherTimer { Interval = NoticeDuration };
            _noticeTimer.Tick += NoticeTimer_Tick;
            DataContextChanged += OnDataContextChanged;
            SizeChanged += (_, e) => UpdateLayoutWidths(e.NewSize.Width);

            Loaded += (_, _) =>
            {
                SetScreenActive(true);
                UpdateLayoutWidths(ActualWidth);
                BindDiagramThemeService();
            };
            Unloaded += (_, _) => SetScreenActive(false);
        }

        #endregion

        public double RowWidth
        {
            get => (double)GetValue(RowWidthProperty);
            set => SetValue(RowWidthProperty, value);
        }

        public double CardWidth
        {
            get => (double)GetValue(CardWidthProperty);
            set => SetValue(CardWidthProperty, value);
        }

        public double CardRowWidth
        {
            get => (double)GetValue(CardRowWidthProperty);
            set => SetValue(CardRowWidthProperty, value);
        }

        internal static double ColumnWidth(double available) => ColumnWidth(available, MinimumRowWidth);

        internal static double ColumnWidth(double available, double minimum)
        {
            int columns = Math.Max(1, (int)Math.Floor(available / (minimum + RowGap)));
            return Math.Max(minimum, available / columns);
        }

        private void BindDiagramThemeService()
        {
            if (FindName("ControllerDiagram") is not ControllerDiagramView diagram)
                return;

            if (Application.Current is IServiceProvider sp && sp.GetService(typeof(ThemeService)) is ThemeService themeService)
                diagram.ThemeService = themeService;
        }

        private void UpdateLayoutWidths(double pageWidth)
        {
            if (pageWidth <= 0)
                return;

            bool isLandscapeTwoColumn = pageWidth >= TwoColumnBreakpointWidth;

            if (isLandscapeTwoColumn)
            {
                ControllerColumn.Width = new GridLength(LandscapeControllerColumnWidth);
                DetailsColumn.Width = new GridLength(1, GridUnitType.Star);
                Grid.SetColumn(ControllerVisualCard, 0);
                Grid.SetRow(ControllerVisualCard, 0);
                Grid.SetColumn(DetailsHost, 1);
                Grid.SetRow(DetailsHost, 0);
                ControllerVisualCard.Margin = new Thickness(0, 0, 16, 16);
                ControllerVisualCard.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            else
            {
                ControllerColumn.Width = new GridLength(1, GridUnitType.Star);
                DetailsColumn.Width = new GridLength(0);
                Grid.SetColumn(ControllerVisualCard, 0);
                Grid.SetRow(ControllerVisualCard, 0);
                Grid.SetColumn(DetailsHost, 0);
                Grid.SetRow(DetailsHost, 1);
                ControllerVisualCard.Margin = new Thickness(0, 0, 0, 16);
                ControllerVisualCard.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            double rightColumnWidth = isLandscapeTwoColumn
                ? Math.Max(MinimumRowWidth, pageWidth - LandscapeControllerColumnWidth - 16 - SystemParameters.VerticalScrollBarWidth)
                : Math.Max(0, pageWidth - SystemParameters.VerticalScrollBarWidth);

            RowWidth = ColumnWidth(rightColumnWidth);
            CardWidth = ColumnWidth(Math.Min(rightColumnWidth, MaximumCardsWidth), MinimumRowWidth + 2 * CardPadding);
            CardRowWidth = ColumnWidth(Math.Max(0, CardWidth - RowGap - 2 * CardPadding));
            ConfigurationComboBox.Width = Math.Clamp(Math.Max(0, pageWidth - SystemParameters.VerticalScrollBarWidth) - RowGap, 240, MaximumPickerWidth);
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
            if (e.PropertyName == nameof(ControllerSetupViewModel.NoticeText))
            {
                _noticeTimer.Stop();
                if (ViewModel.NoticeText is not null)
                    _noticeTimer.Start();
            }
        }

        private void NoticeTimer_Tick(object? sender, EventArgs e)
        {
            _noticeTimer.Stop();
            ViewModel.DismissNoticeCommand.Execute(null);
        }

        #region Function panel

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

            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }
}