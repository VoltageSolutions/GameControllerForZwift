using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Localization;
using GameControllerForZwift.UI.WPF.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>
    /// The controller illustration. It lights the selected input and any pressed row, moves the sticks, and fills the
    /// triggers from the live controller state. Colors are <c>DynamicResource</c> theme tokens, so it follows the
    /// appearance without any code.
    /// </summary>
    public partial class ControllerDiagramView : UserControl
    {
        // Max stick travel in drawing units (436x367 canvas): inside the 37.5 well radius minus the 28 cap radius.
        internal const double MaxStickTravel = 8.0;

        // Trigger pill bounds in the drawing: y from about 1 to 48.5.
        internal const double TriggerTopY = 1.0;
        internal const double TriggerBottomY = 48.5;

        private const double AxisMax = 32767.0;
        private const string LitBrushKey = "Brush.accent";
        private const string RestBrushKey = "Brush.pad-key";

        public static readonly DependencyProperty SelectedInputProperty =
            DependencyProperty.Register(nameof(SelectedInput), typeof(ControllerInput?), typeof(ControllerDiagramView),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(nameof(Rows), typeof(IEnumerable<ButtonRowViewModel>), typeof(ControllerDiagramView),
                new PropertyMetadata(null, OnRowsChanged));

        public static readonly DependencyProperty ControllerValuesProperty =
            DependencyProperty.Register(nameof(ControllerValues), typeof(ControllerData), typeof(ControllerDiagramView),
                new PropertyMetadata(null, OnVisualStateChanged));

        private readonly Dictionary<ControllerInput, Shape> _inputHighlights = [];
        private IEnumerable<ButtonRowViewModel>? _subscribedRows;
        private bool _refreshPending;

        public ControllerDiagramView()
        {
            InitializeComponent();
            BuildInputHighlightMap();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>The input drawn lit as the selected one, or null for none.</summary>
        public ControllerInput? SelectedInput
        {
            get => (ControllerInput?)GetValue(SelectedInputProperty);
            set => SetValue(SelectedInputProperty, value);
        }

        /// <summary>The Buttons rows. A row that reports <see cref="ButtonRowViewModel.IsPressed"/> lights its input.</summary>
        public IEnumerable<ButtonRowViewModel>? Rows
        {
            get => (IEnumerable<ButtonRowViewModel>?)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        /// <summary>The live controller state that moves the sticks and fills the triggers, or null for the resting pose.</summary>
        public ControllerData? ControllerValues
        {
            get => (ControllerData?)GetValue(ControllerValuesProperty);
            set => SetValue(ControllerValuesProperty, value);
        }

        /// <summary>Where a stick cap sits for a thumbstick reading, in drawing units from its resting place.</summary>
        internal static double StickOffset(double axis) =>
            Math.Clamp(axis / AxisMax, -1.0, 1.0) * MaxStickTravel;

        /// <summary>The share of a trigger that is pulled, 0 to 1.</summary>
        internal static double TriggerRatio(double value) =>
            Math.Clamp(value / AxisMax, 0.0, 1.0);

        /// <summary>The clip that shows the pulled part of a trigger, rising from the bottom of the pill.</summary>
        internal static Rect TriggerClip(Rect current, double ratio)
        {
            double fillHeight = (TriggerBottomY - TriggerTopY) * Math.Clamp(ratio, 0.0, 1.0);
            return new Rect(current.X, TriggerBottomY - fillHeight, current.Width, fillHeight);
        }

        /// <summary>Redraws now. Property changes call this once per dispatcher turn.</summary>
        internal void RefreshDiagram()
        {
            _refreshPending = false;

            foreach (var (input, shape) in _inputHighlights)
                shape.SetResourceReference(Shape.FillProperty, IsLit(input) ? LitBrushKey : RestBrushKey);

            ControllerData? values = ControllerValues;
            LeftStickTransform.X = StickOffset(values?.LeftThumbstickX ?? 0);
            LeftStickTransform.Y = StickOffset(values?.LeftThumbstickY ?? 0);
            RightStickTransform.X = StickOffset(values?.RightThumbstickX ?? 0);
            RightStickTransform.Y = StickOffset(values?.RightThumbstickY ?? 0);
            LeftTriggerClip.Rect = TriggerClip(LeftTriggerClip.Rect, TriggerRatio(values?.LeftTrigger ?? 0));
            RightTriggerClip.Rect = TriggerClip(RightTriggerClip.Rect, TriggerRatio(values?.RightTrigger ?? 0));
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ControllerDiagramViewAutomationPeer(this);

        private bool IsLit(ControllerInput input)
        {
            if (SelectedInput == input)
                return true;

            return _subscribedRows?.Any(row => row.Input == input && row.IsPressed) == true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SubscribeRows(Rows);
            ScheduleRefresh();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => UnsubscribeRows();

        private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((ControllerDiagramView)d).ScheduleRefresh();

        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ControllerDiagramView)d;
            view.SubscribeRows(e.NewValue as IEnumerable<ButtonRowViewModel>);
            view.ScheduleRefresh();
        }

        private void SubscribeRows(IEnumerable<ButtonRowViewModel>? rows)
        {
            UnsubscribeRows();

            _subscribedRows = rows;
            if (rows is null)
                return;

            if (rows is INotifyCollectionChanged collectionChanged)
                collectionChanged.CollectionChanged += OnRowsCollectionChanged;

            foreach (var row in rows)
                row.PropertyChanged += OnRowPropertyChanged;
        }

        private void UnsubscribeRows()
        {
            if (_subscribedRows is null)
                return;

            if (_subscribedRows is INotifyCollectionChanged collectionChanged)
                collectionChanged.CollectionChanged -= OnRowsCollectionChanged;

            foreach (var row in _subscribedRows)
                row.PropertyChanged -= OnRowPropertyChanged;

            _subscribedRows = null;
        }

        private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // A reset leaves no list of what went, so listen again to what is there now.
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SubscribeRows(Rows);
            }
            else
            {
                if (e.OldItems is not null)
                {
                    foreach (ButtonRowViewModel row in e.OldItems)
                        row.PropertyChanged -= OnRowPropertyChanged;
                }
                if (e.NewItems is not null)
                {
                    foreach (ButtonRowViewModel row in e.NewItems)
                        row.PropertyChanged += OnRowPropertyChanged;
                }
            }
            ScheduleRefresh();
        }

        private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ButtonRowViewModel.IsPressed) or nameof(ButtonRowViewModel.Input))
                ScheduleRefresh();
        }

        // Many changes in one turn (every row, every controller poll) redraw once.
        private void ScheduleRefresh()
        {
            if (_refreshPending)
                return;

            _refreshPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Render, RefreshDiagram);
        }

        private void BuildInputHighlightMap()
        {
            _inputHighlights[ControllerInput.A] = ButtonAHighlight;
            _inputHighlights[ControllerInput.B] = ButtonBHighlight;
            _inputHighlights[ControllerInput.X] = ButtonXHighlight;
            _inputHighlights[ControllerInput.Y] = ButtonYHighlight;
            _inputHighlights[ControllerInput.LeftBumper] = LeftBumperHighlight;
            _inputHighlights[ControllerInput.RightBumper] = RightBumperHighlight;
            _inputHighlights[ControllerInput.DPadUp] = DPadUpHighlight;
            _inputHighlights[ControllerInput.DPadDown] = DPadDownHighlight;
            _inputHighlights[ControllerInput.DPadLeft] = DPadLeftHighlight;
            _inputHighlights[ControllerInput.DPadRight] = DPadRightHighlight;
            _inputHighlights[ControllerInput.LeftThumbstickClick] = LeftStickCap;
            _inputHighlights[ControllerInput.RightThumbstickClick] = RightStickCap;
            _inputHighlights[ControllerInput.View] = ViewButtonHighlight;
            _inputHighlights[ControllerInput.Menu] = MenuButtonHighlight;
        }

        /// <summary>Reports the illustration as an image named "Your controller" (accessibility spec, <c>ButtonsControllerImage</c>).</summary>
        private sealed class ControllerDiagramViewAutomationPeer(ControllerDiagramView owner) : FrameworkElementAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

            protected override string GetClassNameCore() => nameof(ControllerDiagramView);

            protected override string GetNameCore()
            {
                string name = base.GetNameCore();
                return string.IsNullOrEmpty(name) ? Localizer.Get("ButtonsControllerImageName") : name;
            }
        }
    }
}
