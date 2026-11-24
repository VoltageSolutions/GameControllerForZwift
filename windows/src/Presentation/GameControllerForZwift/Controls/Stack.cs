using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>
    /// A vertical or horizontal list with a gap between its children, from a spacing token (docs/spec/ux/style-system.md).
    /// WPF's <see cref="StackPanel"/> has no spacing. A collapsed child takes no gap. A horizontal stack may wrap, with
    /// the gap between the rows too.
    /// </summary>
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Stack is the name of the component in the style system (docs/spec/ux/style-system.md), not a collection.")]
    public class Stack : Panel
    {
        /// <summary>Identifies <see cref="Orientation"/>.</summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(Stack),
            new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Identifies <see cref="Gap"/>.</summary>
        public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
            nameof(Gap), typeof(double), typeof(Stack),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Identifies <see cref="Wrap"/>.</summary>
        public static readonly DependencyProperty WrapProperty = DependencyProperty.Register(
            nameof(Wrap), typeof(bool), typeof(Stack),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Gets or sets the direction of the list.</summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>Gets or sets the space between children.</summary>
        public double Gap
        {
            get => (double)GetValue(GapProperty);
            set => SetValue(GapProperty, value);
        }

        /// <summary>Gets or sets whether a horizontal stack starts a new row when a child does not fit.</summary>
        public bool Wrap
        {
            get => (bool)GetValue(WrapProperty);
            set => SetValue(WrapProperty, value);
        }

        private IEnumerable<UIElement> Shown => InternalChildren.Cast<UIElement>().Where(c => c.Visibility != Visibility.Collapsed);

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Orientation == Orientation.Vertical)
            {
                double width = 0;
                double height = 0;
                foreach (UIElement child in Shown)
                {
                    child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                    width = Math.Max(width, child.DesiredSize.Width);
                    height += child.DesiredSize.Height + (height > 0 ? Gap : 0);
                }

                return new Size(width, height);
            }

            return Wrap ? MeasureWrapped(availableSize) : MeasureRow(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Orientation == Orientation.Vertical)
            {
                double y = 0;
                foreach (UIElement child in Shown)
                {
                    child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                    y += child.DesiredSize.Height + Gap;
                }

                return finalSize;
            }

            if (Wrap)
            {
                return ArrangeWrapped(finalSize);
            }

            double x = 0;
            foreach (UIElement child in Shown)
            {
                child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                x += child.DesiredSize.Width + Gap;
            }

            return finalSize;
        }

        private Size MeasureRow(Size availableSize)
        {
            double width = 0;
            double height = 0;
            foreach (UIElement child in Shown)
            {
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                width += child.DesiredSize.Width + (width > 0 ? Gap : 0);
                height = Math.Max(height, child.DesiredSize.Height);
            }

            return new Size(width, height);
        }

        private Size MeasureWrapped(Size availableSize)
        {
            double limit = availableSize.Width;
            double widest = 0;
            double x = 0;
            double rowHeight = 0;
            double height = 0;
            foreach (UIElement child in Shown)
            {
                child.Measure(new Size(limit, availableSize.Height));
                double next = x + (x > 0 ? Gap : 0) + child.DesiredSize.Width;
                if (x > 0 && next > limit)
                {
                    height += rowHeight + Gap;
                    rowHeight = 0;
                    next = child.DesiredSize.Width;
                }

                x = next;
                widest = Math.Max(widest, x);
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }

            return new Size(widest, height + rowHeight);
        }

        // Children of a row are arranged over the height of the row, so their own vertical alignment centers them.
        private Size ArrangeWrapped(Size finalSize)
        {
            var row = new List<UIElement>();
            double x = 0;
            double y = 0;
            double rowHeight = 0;
            foreach (UIElement child in Shown)
            {
                double next = x + (x > 0 ? Gap : 0) + child.DesiredSize.Width;
                if (x > 0 && next > finalSize.Width)
                {
                    ArrangeRow(row, y, rowHeight);
                    row.Clear();
                    y += rowHeight + Gap;
                    x = 0;
                    rowHeight = 0;
                    next = child.DesiredSize.Width;
                }

                row.Add(child);
                x = next;
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }

            ArrangeRow(row, y, rowHeight);
            return finalSize;
        }

        private void ArrangeRow(List<UIElement> row, double y, double rowHeight)
        {
            double left = 0;
            foreach (UIElement child in row)
            {
                child.Arrange(new Rect(left, y, child.DesiredSize.Width, rowHeight));
                left += child.DesiredSize.Width + Gap;
            }
        }
    }
}
