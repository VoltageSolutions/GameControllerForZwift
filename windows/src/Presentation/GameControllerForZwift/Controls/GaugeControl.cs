using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameControllerForZwift.UI.WPF.Controls
{
    public class GaugeControl : UserControl
    {
        private readonly Grid _grid;
        private readonly Rectangle _backgroundBar;
        private readonly Rectangle _fillBar;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(double), typeof(GaugeControl),
            new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(MaxValue), typeof(double), typeof(GaugeControl),
            new PropertyMetadata(65535.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GaugeControl gauge)
            {
                gauge.UpdateFillBar();
            }
        }

        public GaugeControl()
        {
            _grid = new Grid
            {
                Background = Brushes.Transparent
            };

            // Background bar
            _backgroundBar = new Rectangle
            {
                Height = 20,
                RadiusX = 5,
                RadiusY = 5,
                Opacity = 1
            };
            // Resource references follow the light/dark tokens dynamically.
            _backgroundBar.SetResourceReference(Shape.FillProperty, "Brush.surface-2");
            _grid.Children.Add(_backgroundBar);

            // Fill bar (dynamic)
            _fillBar = new Rectangle
            {
                Height = 20,
                RadiusX = 5,
                RadiusY = 5,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            _fillBar.SetResourceReference(Shape.FillProperty, "Brush.accent");
            _grid.Children.Add(_fillBar);

            Content = _grid;

            Loaded += (s, e) => UpdateFillBar();
            SizeChanged += (s, e) => UpdateFillBar();
        }

        private void UpdateFillBar()
        {
            if (_grid.ActualWidth <= 0) return;

            double clampedValue = Math.Max(0, Math.Min(MaxValue, Value));
            double fillRatio = clampedValue / MaxValue;
            double fillWidth = _grid.ActualWidth * fillRatio;

            _fillBar.Width = fillWidth;
        }
    }
}
