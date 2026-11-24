using GameControllerForZwift.Core;
using GameControllerForZwift.UI.WPF.Controls;
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

        internal const double MaximumPickerWidth = 420;
        private const double TwoColumnBreakpointWidth = 900;
        private const double LandscapeControllerColumnWidth = 376;

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
            };
            Unloaded += (_, _) =>
            {
                SetScreenActive(false);
                MoveSheetBack();
            };
        }

        #endregion

        private void UpdateLayoutWidths(double pageWidth)
        {
            if (pageWidth <= 0)
                return;

            // The gap below and beside the controller card is the card gap of the size class.
            double cardGap = TryFindResource("Layout.CardGap") as double? ?? 0;
            bool isLandscapeTwoColumn = pageWidth >= TwoColumnBreakpointWidth;

            if (isLandscapeTwoColumn)
            {
                ControllerColumn.Width = new GridLength(LandscapeControllerColumnWidth);
                DetailsColumn.Width = new GridLength(1, GridUnitType.Star);
                Grid.SetColumn(ControllerVisualCard, 0);
                Grid.SetRow(ControllerVisualCard, 0);
                Grid.SetColumn(DetailsHost, 1);
                Grid.SetRow(DetailsHost, 0);
                ControllerVisualCard.Margin = new Thickness(0, 0, cardGap, cardGap);
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
                ControllerVisualCard.Margin = new Thickness(0, 0, 0, cardGap);
                ControllerVisualCard.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            // The picker never grows wider than the page minus its padding.
            double padding = TryFindResource("Layout.PagePadding") is Thickness page ? page.Left + page.Right : 0;
            ConfigurationComboBox.Width = Math.Clamp(pageWidth - padding, 240, MaximumPickerWidth);
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
                MoveSheetToWindow();
                Dispatcher.BeginInvoke(DispatcherPriority.Input, () => FunctionPanel.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
            }
            else
            {
                MoveSheetBack();
                if (_focusBeforePanel is UIElement { IsVisible: true } previous)
                {
                    Keyboard.Focus(previous);
                    _focusBeforePanel = null;
                }
            }
        }

        // The sheet covers the navigation and the More button, so while it is open it lives in the window's overlay layer
        // (UX-N-013). It keeps its data context and its names, and it comes back to the page when it closes. Without a
        // main window, for example when the page is hosted alone, it stays in the page.
        private void MoveSheetToWindow()
        {
            if (Window.GetWindow(this) is MainWindow window && FunctionOverlay.Parent == PageRoot)
            {
                PageRoot.Children.Remove(FunctionOverlay);
                FunctionOverlay.DataContext = DataContext;
                window.ShowOverlay(FunctionOverlay);
            }
        }

        private void MoveSheetBack()
        {
            if (FunctionOverlay.Parent is Panel parent && parent != PageRoot)
            {
                parent.Children.Remove(FunctionOverlay);
                FunctionOverlay.ClearValue(DataContextProperty);
                PageRoot.Children.Add(FunctionOverlay);
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