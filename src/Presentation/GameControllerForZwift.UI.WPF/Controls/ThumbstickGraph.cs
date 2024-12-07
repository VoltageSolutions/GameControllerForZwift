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
        //private Ellipse _circle;
        private List<Ellipse> _circles;
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
                graph.UpdateCircleAndBounds();
            }
        }

        public ThumbstickGraph()
        {
            _chartCanvas = new Canvas();
            Content = _chartCanvas;

            _circles = new List<Ellipse>();

            for (int i = 0; i < 4; i++)
            {
                var circle = new Ellipse
                {
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 },
                    Stroke = Brushes.Black
                };
                _circles.Add(circle);
                _chartCanvas.Children.Add(circle);
            }

            _plotPoint = new Ellipse
            {
                Width = 10,
                Height = 10,
            };
            _chartCanvas.Children.Add(_plotPoint);

            Loaded += (s, e) => UpdateCircleAndBounds();
            SizeChanged += (s, e) => UpdateCircleAndBounds();
        }

        private void UpdateCircleAndBounds()
        {
            if (_chartCanvas == null || _circles == null) return;

            double canvasSquareLength = _chartCanvas.ActualWidth;

            if (canvasSquareLength <= 0) return;

            double centerX = canvasSquareLength / 2;
            double centerY = centerX;

            double maxRadius = canvasSquareLength / 2 - 8; // Maximum radius within bounds

            for (int i = 0; i < _circles.Count; i++)
            {
                double radius = maxRadius * (1 - (i * 0.25)); // Each circle is 25% smaller than the previous one

                var circle = _circles[i];
                circle.Width = radius * 2;
                circle.Height = radius * 2;

                Canvas.SetLeft(circle, centerX - radius);
                Canvas.SetTop(circle, centerY - radius);

                circle.Fill = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
                circle.Stroke = Brushes.Black;
            }

            UpdatePlotPosition();
        }

        //private void UpdatePlotPosition()
        //{
        //    if (_chartCanvas == null || _plotPoint == null) return;

        //    double canvasWidth = _chartCanvas.ActualWidth;
        //    double canvasHeight = _chartCanvas.ActualHeight;

        //    if (canvasWidth <= 0 || canvasHeight <= 0) return;

        //    // Clamp position values within the bounds
        //    double clampedX = Math.Max(MinX, Math.Min(MaxX, Position.X));
        //    double clampedY = Math.Max(MinY, Math.Min(MaxY, Position.Y));

        //    // Scale position values to canvas coordinates
        //    double scaleX = canvasWidth / (MaxX - MinX);
        //    double scaleY = canvasHeight / (MaxY - MinY);

        //    // X-coordinate on the canvas
        //    double canvasX = (clampedX - MinX) * scaleX;

        //    // Y-coordinate on the canvas (invert Y-axis logic)
        //    double canvasY = canvasHeight - (clampedY - MinY) * scaleY;

        //    // Update plot point position
        //    Canvas.SetLeft(_plotPoint, canvasX - _plotPoint.Width / 2);
        //    Canvas.SetBottom(_plotPoint, canvasY - _plotPoint.Height / 2);
        //}

        private void UpdatePlotPosition()
        {
            if (_chartCanvas == null || _plotPoint == null) return;

            //double canvasWidth = _chartCanvas.ActualWidth;
            //double canvasHeight = _chartCanvas.ActualHeight;
            double canvasSquareLength = _chartCanvas.ActualWidth;

            if (canvasSquareLength <= 0) return;

            double originX = MaxX / 2;
            double originY = MaxY / 2;

            // Clamp position values within the bounds - graphed position will not exceed these bounds
            double clampedX = Math.Max(MinX, Math.Min(MaxX, Position.X));
            double clampedY = Math.Max(MinY, Math.Min(MaxY, Position.Y));

            double scaleX = canvasSquareLength / (MaxX - MinX);
            double scaleY = canvasSquareLength / (MaxY - MinY);
            //double scale = Math.Min(scaleX, scaleY);

            // X-coordinate on the canvas
            double canvasX = (clampedX - MinX) * scaleX;

            // Y-coordinate on the canvas (invert Y-axis logic)
            double canvasY = (clampedY - MinY) * scaleY;

            // Update plot point position
            Canvas.SetLeft(_plotPoint, canvasX - _plotPoint.Width / 2);
            Canvas.SetTop(_plotPoint, canvasY - _plotPoint.Height / 2);

            // Update plot point color
            if (IsOutsideOrigin(originX, Position.X) || IsOutsideOrigin(originY, Position.Y))
            {
                _plotPoint.Fill = (Brush)Application.Current.Resources["AccentTextFillColorTertiaryBrush"];
            }
            else
            {
                
                _plotPoint.Fill = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
            }
        }

        public bool IsOutsideOrigin(double origin, double position)
        {
            double factor = .1;
            if ((Math.Abs(origin - position) / origin) > factor)
                return true;
            else 
                return false;
        }



    }

}