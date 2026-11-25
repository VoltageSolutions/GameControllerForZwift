using GameControllerForZwift.Core.Controller;
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
    /// triggers from the live controller state. The drawing is the SVG for <see cref="Kind"/> (UX-V-059), and its colors
    /// are <c>DynamicResource</c> theme tokens, so it follows the appearance without any code.
    /// </summary>
    public partial class ControllerDiagramView : UserControl
    {
        // Max stick travel in drawing units: inside the 37.5 well radius minus the 28 cap radius.
        internal const double MaxStickTravel = 8.0;

        private const double AxisMax = 32767.0;
        private const string LitBrushKey = "Brush.accent";
        private const string RestBrushKey = "Brush.art-shade";

        public static readonly DependencyProperty KindProperty =
            DependencyProperty.Register(nameof(Kind), typeof(ControllerKind), typeof(ControllerDiagramView),
                new PropertyMetadata(ControllerKind.Generic, OnKindChanged));

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
        private ControllerSvgDrawing? _drawing;
        private IEnumerable<ButtonRowViewModel>? _subscribedRows;
        private bool _refreshPending;

        public ControllerDiagramView()
        {
            InitializeComponent();
            LoadDrawing();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>The kind of controller drawn. Changing it swaps the illustration and the accessible name.</summary>
        public ControllerKind Kind
        {
            get => (ControllerKind)GetValue(KindProperty);
            set => SetValue(KindProperty, value);
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
        internal static double StickOffset(double axis) => StickOffset(axis, MaxStickTravel);

        /// <summary>Where a stick cap sits for a reading, for a drawing whose cap travels <paramref name="travel"/> units.</summary>
        internal static double StickOffset(double axis, double travel) =>
            Math.Clamp(axis / AxisMax, -1.0, 1.0) * travel;

        /// <summary>The share of a trigger that is pulled, 0 to 1.</summary>
        internal static double TriggerRatio(double value) =>
            Math.Clamp(value / AxisMax, 0.0, 1.0);

        /// <summary>The clip that shows the pulled part of a trigger fill, rising from the bottom of its bounds.</summary>
        internal static Rect TriggerClip(Rect bounds, double ratio)
        {
            double fillHeight = bounds.Height * Math.Clamp(ratio, 0.0, 1.0);
            return new Rect(bounds.X, bounds.Bottom - fillHeight, bounds.Width, fillHeight);
        }

        /// <summary>The shape that lights for an input in the current drawing, or null when this kind has no such shape.</summary>
        internal Shape? InputShape(ControllerInput input) => _inputHighlights.GetValueOrDefault(input);

        /// <summary>The drawing's element with an SVG id, or null.</summary>
        internal FrameworkElement? Element(string id) => _drawing?.ById.GetValueOrDefault(id);

        internal TranslateTransform? LeftStickTransform => _drawing?.LeftStick;
        internal TranslateTransform? RightStickTransform => _drawing?.RightStick;

        /// <summary>The visible clip of a trigger fill (height 0 at rest), or null when the kind has no analog fill.</summary>
        internal Rect? LeftTriggerClipRect => ClipOf(_drawing?.LeftTriggerFill);
        internal Rect? RightTriggerClipRect => ClipOf(_drawing?.RightTriggerFill);

        private static Rect? ClipOf(Path? fill) => (fill?.Clip as RectangleGeometry)?.Rect;

        /// <summary>Redraws now. Property changes call this once per dispatcher turn.</summary>
        internal void RefreshDiagram()
        {
            _refreshPending = false;

            foreach (var (input, shape) in _inputHighlights)
            {
                bool lit = IsLit(input);
                string rest = _drawing?.RestBrushes.GetValueOrDefault(input.ToString()) ?? RestBrushKey;
                shape.SetResourceReference(Shape.FillProperty, lit ? LitBrushKey : rest);
                if (_drawing?.ArmIds.Contains(input.ToString()) == true)
                {
                    if (lit) shape.SetResourceReference(Shape.StrokeProperty, "Brush.art-line");
                    else shape.Stroke = null;
                }
                if (_drawing?.GlyphFor.GetValueOrDefault(input.ToString()) is { } glyph)
                    glyph.SetResourceReference(Shape.StrokeProperty, lit ? ControllerSvgLoader.GlyphLitBrushKey : ControllerSvgLoader.GlyphBrushKey);
            }

            ControllerData? values = ControllerValues;
            Move(_drawing?.LeftStick, _drawing?.LeftTravel ?? MaxStickTravel, values?.LeftThumbstickX ?? 0, values?.LeftThumbstickY ?? 0);
            Move(_drawing?.RightStick, _drawing?.RightTravel ?? MaxStickTravel, values?.RightThumbstickX ?? 0, values?.RightThumbstickY ?? 0);
            Fill(_drawing?.LeftTriggerFill, _drawing?.LeftTriggerFillBounds, values?.LeftTrigger ?? 0);
            Fill(_drawing?.RightTriggerFill, _drawing?.RightTriggerFillBounds, values?.RightTrigger ?? 0);
        }

        private static void Move(TranslateTransform? stick, double travel, double x, double y)
        {
            if (stick is null)
                return;
            stick.X = StickOffset(x, travel);
            stick.Y = StickOffset(y, travel);
        }

        // Digital triggers have no fill shape, so there is nothing to fill; they only light.
        private static void Fill(Path? fill, Rect? bounds, double value)
        {
            if (fill is not null && bounds is { IsEmpty: false } rect)
                fill.Clip = new RectangleGeometry(TriggerClip(rect, TriggerRatio(value)));
        }

        /// <summary>Replaces the drawing with the one for <see cref="Kind"/> and finds the shapes that light.</summary>
        private void LoadDrawing()
        {
            _drawing = ControllerSvgLoader.TryLoad(Kind);
            DiagramViewbox.Child = _drawing?.Root;
            BuildInputHighlightMap();

            // Start every trigger fill empty until the first refresh.
            foreach (Path fill in new[] { _drawing?.LeftTriggerFill, _drawing?.RightTriggerFill }.OfType<Path>().Where(fill => fill.Data is not null))
                fill.Clip = new RectangleGeometry(new Rect(fill.Data.Bounds.X, fill.Data.Bounds.Bottom, fill.Data.Bounds.Width, 0));
        }

        private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ControllerDiagramView)d;
            view.LoadDrawing();
            UIElementAutomationPeer.FromElement(view)?.InvalidatePeer();
            view.ScheduleRefresh();
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ControllerDiagramViewAutomationPeer(this);

        private bool IsLit(ControllerInput input)
        {
            if (SelectedInput == input)
                return true;

            // An analog trigger shows how far it is pulled with its fill (UX-V-063), so being pulled does not light its whole outline.
            if (HasAnalogFill(input))
                return false;

            return _subscribedRows?.Any(row => row.Input == input && row.IsPressed) == true;
        }

        private bool HasAnalogFill(ControllerInput input) => input switch
        {
            ControllerInput.LeftTrigger => _drawing?.LeftTriggerFill is not null,
            ControllerInput.RightTrigger => _drawing?.RightTriggerFill is not null,
            _ => false,
        };

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

        // An input with no shape in this kind's drawing is simply never lit.
        private void BuildInputHighlightMap()
        {
            _inputHighlights.Clear();
            if (_drawing is null)
                return;

            var shapes = Enum.GetValues<ControllerInput>()
                .Select(input => (Input: input, Shape: _drawing.ById.GetValueOrDefault(input.ToString()) as Shape))
                .Where(pair => pair.Shape is not null);
            foreach (var (input, shape) in shapes)
                _inputHighlights[input] = shape!;
        }

        /// <summary>Reports the illustration as an image named for the kind, such as "Your Xbox controller" (SCR-BTN-047).</summary>
        private sealed class ControllerDiagramViewAutomationPeer(ControllerDiagramView owner) : FrameworkElementAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

            protected override string GetClassNameCore() => nameof(ControllerDiagramView);

            protected override string GetNameCore()
            {
                string name = base.GetNameCore();
                return string.IsNullOrEmpty(name) ? ControllerKindNames.GetAccessibleName(owner.Kind) : name;
            }
        }
    }
}
