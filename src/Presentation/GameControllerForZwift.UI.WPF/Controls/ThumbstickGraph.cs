using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.Controls
{
    public class ThumbstickGraph : UserControl
    {
        private Canvas _chartCanvas;
        private Polygon _octagon;
        private Ellipse _plotPoint;

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position), typeof(Point), typeof(ThumbstickGraph),
            new PropertyMetadata(new Point(0, 0), OnPositionChanged));

        public static readonly DependencyProperty MinXProperty = DependencyProperty.Register(
            nameof(MinX), typeof(double), typeof(ThumbstickGraph),
            new PropertyMetadata(-1.0, OnBoundsChanged));

        public static readonly DependencyProperty MaxXProperty = DependencyProperty.Register(
            nameof(MaxX), typeof(double), typeof(ThumbstickGraph),
            new PropertyMetadata(1.0, OnBoundsChanged));

        public static readonly DependencyProperty MinYProperty = DependencyProperty.Register(
            nameof(MinY), typeof(double), typeof(ThumbstickGraph),
            new PropertyMetadata(-1.0, OnBoundsChanged));

        public static readonly DependencyProperty MaxYProperty = DependencyProperty.Register(
            nameof(MaxY), typeof(double), typeof(ThumbstickGraph),
            new PropertyMetadata(1.0, OnBoundsChanged));

        public Point Position
        {
            get => (Point)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public double MinX
        {
            get => (double)GetValue(MinXProperty);
            set => SetValue(MinXProperty, value);
        }

        public double MaxX
        {
            get => (double)GetValue(MaxXProperty);
            set => SetValue(MaxXProperty, value);
        }

        public double MinY
        {
            get => (double)GetValue(MinYProperty);
            set => SetValue(MinYProperty, value);
        }

        public double MaxY
        {
            get => (double)GetValue(MaxYProperty);
            set => SetValue(MaxYProperty, value);
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThumbstickGraph graph)
            {
                graph.UpdatePlotPosition();
            }
        }

        private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThumbstickGraph graph)
            {
                graph.UpdateOctagonAndBounds();
            }
        }

        public ThumbstickGraph()
        {
            _chartCanvas = new Canvas();
            Content = _chartCanvas;

            _octagon = new Polygon
            {
                Stroke = Brushes.Black,
                Fill = Brushes.Gray,
                StrokeThickness = 1
            };
            _chartCanvas.Children.Add(_octagon);

            _plotPoint = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red
            };
            _chartCanvas.Children.Add(_plotPoint);

            Loaded += (s, e) => UpdateOctagonAndBounds();
            SizeChanged += (s, e) => UpdateOctagonAndBounds();
        }

        private void UpdateOctagonAndBounds()
        {
            if (_chartCanvas == null || _octagon == null) return;

            double canvasWidth = _chartCanvas.ActualWidth;
            double canvasHeight = _chartCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            double scaleX = canvasWidth / (MaxX - MinX);
            double scaleY = canvasHeight / (MaxY - MinY);
            double scale = Math.Min(scaleX, scaleY);

            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;

            double radius = scale * (MaxX - MinX) / 2;

            _octagon.Points = new PointCollection
            {
                new Point(centerX, centerY - radius),                         // Top
                new Point(centerX + radius * 0.707, centerY - radius * 0.707), // Top-right
                new Point(centerX + radius, centerY),                         // Right
                new Point(centerX + radius * 0.707, centerY + radius * 0.707), // Bottom-right
                new Point(centerX, centerY + radius),                         // Bottom
                new Point(centerX - radius * 0.707, centerY + radius * 0.707), // Bottom-left
                new Point(centerX - radius, centerY),                         // Left
                new Point(centerX - radius * 0.707, centerY - radius * 0.707)  // Top-left
            };

            UpdatePlotPosition();
        }

        private void UpdatePlotPosition()
        {
            if (_chartCanvas == null || _plotPoint == null) return;

            double canvasWidth = _chartCanvas.ActualWidth;
            double canvasHeight = _chartCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            // Clamp position values within the bounds
            double clampedX = Math.Max(MinX, Math.Min(MaxX, Position.X));
            double clampedY = Math.Max(MinY, Math.Min(MaxY, Position.Y));

            // Scale position values to canvas coordinates
            double scaleX = canvasWidth / (MaxX - MinX);
            double scaleY = canvasHeight / (MaxY - MinY);

            // X-coordinate on the canvas
            double canvasX = (clampedX - MinX) * scaleX;

            // Y-coordinate on the canvas (invert Y-axis logic)
            double canvasY = canvasHeight - (clampedY - MinY) * scaleY;

            // Update plot point position
            Canvas.SetLeft(_plotPoint, canvasX - _plotPoint.Width / 2);
            Canvas.SetBottom(_plotPoint, canvasY - _plotPoint.Height / 2);
        }



    }

}