using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Theme;
using GameControllerForZwift.UI.WPF.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>
    /// Reusable live SVG controller diagram that tracks button states, stick offsets,
    /// analog triggers, and theme changes across screens.
    /// </summary>
    public partial class ControllerDiagramView : UserControl
    {
        // Max stick travel in SVG units (436x367 viewBox): well inside the 37.5px well radius minus 28px cap radius.
        private const double MaxStickTravelSvgUnits = 8.0;

        // Trigger pill bounds in controller.svg (436x367 viewBox): y from ~1 to ~48.5.
        private const double TriggerTopY = 1.0;
        private const double TriggerBottomY = 48.5;
        private const double TriggerMaxValue = 32767.0;

        public static readonly DependencyProperty SelectedInputProperty =
            DependencyProperty.Register(nameof(SelectedInput), typeof(ControllerInput?), typeof(ControllerDiagramView),
                new PropertyMetadata(null, OnVisualStateChanged));

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(nameof(Rows), typeof(IEnumerable<ButtonRowViewModel>), typeof(ControllerDiagramView),
                new PropertyMetadata(null, OnRowsChanged));

        public static readonly DependencyProperty ControllerValuesProperty =
            DependencyProperty.Register(nameof(ControllerValues), typeof(object), typeof(ControllerDiagramView),
                new PropertyMetadata(null, OnControllerValuesChanged));

        private readonly Dictionary<ControllerInput, Shape> _inputHighlights = new();
        private IEnumerable<ButtonRowViewModel>? _subscribedRows;
        private INotifyPropertyChanged? _subscribedValues;
        private ThemeService? _subscribedThemeService;

        public ControllerDiagramView()
        {
            InitializeComponent();
            BuildInputHighlightMap();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public ControllerInput? SelectedInput
        {
            get => (ControllerInput?)GetValue(SelectedInputProperty);
            set => SetValue(SelectedInputProperty, value);
        }

        public IEnumerable<ButtonRowViewModel>? Rows
        {
            get => (IEnumerable<ButtonRowViewModel>?)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public object? ControllerValues
        {
            get => GetValue(ControllerValuesProperty);
            set => SetValue(ControllerValuesProperty, value);
        }

        public ThemeService? ThemeService
        {
            get => _subscribedThemeService;
            set
            {
                if (ReferenceEquals(_subscribedThemeService, value))
                    return;

                if (_subscribedThemeService is not null)
                    _subscribedThemeService.TokensChanged -= OnTokensChanged;

                _subscribedThemeService = value;

                if (_subscribedThemeService is not null)
                    _subscribedThemeService.TokensChanged += OnTokensChanged;

                ScheduleRefresh();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => ScheduleRefresh();

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeRows();
            UnsubscribeValues();
        }

        private void OnTokensChanged(object? sender, EventArgs e) => ScheduleRefresh();

        private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ControllerDiagramView)d).ScheduleRefresh();
        }

        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ControllerDiagramView)d;
            view.UnsubscribeRows();
            view.SubscribeRows(e.NewValue as IEnumerable<ButtonRowViewModel>);
            view.ScheduleRefresh();
        }

        private static void OnControllerValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (ControllerDiagramView)d;
            view.UnsubscribeValues();
            view.SubscribeValues(e.NewValue);
            view.ScheduleRefresh();
        }

        private void SubscribeRows(IEnumerable<ButtonRowViewModel>? rows)
        {
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
            ScheduleRefresh();
        }

        private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ButtonRowViewModel.IsPressed) or nameof(ButtonRowViewModel.IsSelected) or nameof(ButtonRowViewModel.Input))
                ScheduleRefresh();
        }

        private void SubscribeValues(object? values)
        {
            if (values is INotifyPropertyChanged npc)
            {
                _subscribedValues = npc;
                _subscribedValues.PropertyChanged += OnValuesPropertyChanged;
            }
        }

        private void UnsubscribeValues()
        {
            if (_subscribedValues is not null)
            {
                _subscribedValues.PropertyChanged -= OnValuesPropertyChanged;
                _subscribedValues = null;
            }
        }

        private void OnValuesPropertyChanged(object? sender, PropertyChangedEventArgs e) => ScheduleRefresh();

        private void ScheduleRefresh()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Render, RefreshDiagram);
                return;
            }
            Dispatcher.BeginInvoke(DispatcherPriority.Render, RefreshDiagram);
        }

        private void BuildInputHighlightMap()
        {
            _inputHighlights.Clear();
            TryRegisterInput(ControllerInput.A, ButtonAHighlight);
            TryRegisterInput(ControllerInput.B, ButtonBHighlight);
            TryRegisterInput(ControllerInput.X, ButtonXHighlight);
            TryRegisterInput(ControllerInput.Y, ButtonYHighlight);
            TryRegisterInput(ControllerInput.LeftBumper, LeftBumperHighlight);
            TryRegisterInput(ControllerInput.RightBumper, RightBumperHighlight);
            TryRegisterInput(ControllerInput.DPadUp, DPadUpHighlight);
            TryRegisterInput(ControllerInput.DPadDown, DPadDownHighlight);
            TryRegisterInput(ControllerInput.DPadLeft, DPadLeftHighlight);
            TryRegisterInput(ControllerInput.DPadRight, DPadRightHighlight);
            TryRegisterInput(ControllerInput.LeftThumbstickClick, LeftStickCap);
            TryRegisterInput(ControllerInput.RightThumbstickClick, RightStickCap);
            TryRegisterInput(ControllerInput.View, ViewButtonHighlight);
            TryRegisterInput(ControllerInput.Menu, MenuButtonHighlight);
        }

        private void TryRegisterInput(ControllerInput input, Shape? shape)
        {
            if (shape is not null && !_inputHighlights.ContainsKey(input))
                _inputHighlights[input] = shape;
        }

        private void RefreshDiagram()
        {
            var accentBrush = (Brush?)TryFindResource("Brush.accent") ?? Brushes.Orange;
            var keyBrush = (Brush?)TryFindResource("Brush.pad-key") ?? Brushes.LightGray;

            foreach (var (input, shape) in _inputHighlights)
            {
                bool active = SelectedInput == input;

                if (!active && _subscribedRows is not null)
                {
                    foreach (var row in _subscribedRows)
                    {
                        if (row.Input == input && row.IsPressed)
                        {
                            active = true;
                            break;
                        }
                    }
                }

                shape.Fill = active ? accentBrush : keyBrush;
            }

            if (ControllerValues is null)
            {
                LeftStickTransform.X = 0;
                LeftStickTransform.Y = 0;
                RightStickTransform.X = 0;
                RightStickTransform.Y = 0;
                SetTriggerClipHeight(LeftTriggerClip, 0);
                SetTriggerClipHeight(RightTriggerClip, 0);
                return;
            }

            double lx = ReadAxis(ControllerValues, "LeftThumbstickX");
            double ly = ReadAxis(ControllerValues, "LeftThumbstickY");
            LeftStickTransform.X = lx * MaxStickTravelSvgUnits;
            LeftStickTransform.Y = ly * MaxStickTravelSvgUnits;

            double rx = ReadAxis(ControllerValues, "RightThumbstickX");
            double ry = ReadAxis(ControllerValues, "RightThumbstickY");
            RightStickTransform.X = rx * MaxStickTravelSvgUnits;
            RightStickTransform.Y = ry * MaxStickTravelSvgUnits;

            double lt = ReadTrigger(ControllerValues, "LeftTrigger");
            double rt = ReadTrigger(ControllerValues, "RightTrigger");
            SetTriggerClipHeight(LeftTriggerClip, lt);
            SetTriggerClipHeight(RightTriggerClip, rt);
        }

        private static double ReadAxis(object source, string propertyName)
        {
            var prop = source.GetType().GetProperty(propertyName);
            if (prop is null)
                return 0;

            var raw = prop.GetValue(source);
            double value = raw switch
            {
                short s => s,
                int i => i,
                double d => d,
                float f => f,
                _ => 0
            };

            return Math.Clamp(value / 32767.0, -1.0, 1.0);
        }

        private static double ReadTrigger(object source, string propertyName)
        {
            var prop = source.GetType().GetProperty(propertyName);
            if (prop is null)
                return 0;

            var raw = prop.GetValue(source);
            double value = raw switch
            {
                short s => s,
                ushort us => us,
                int i => i,
                byte b => b * 128.45,
                double d => d,
                float f => f,
                _ => 0
            };

            return Math.Clamp(value / TriggerMaxValue, 0.0, 1.0);
        }

        private static void SetTriggerClipHeight(RectangleGeometry clip, double ratio)
        {
            double totalHeight = TriggerBottomY - TriggerTopY;
            double fillHeight = totalHeight * ratio;
            double top = TriggerBottomY - fillHeight;
            clip.Rect = new Rect(clip.Rect.X, top, clip.Rect.Width, Math.Max(0, fillHeight));
        }
    }
}