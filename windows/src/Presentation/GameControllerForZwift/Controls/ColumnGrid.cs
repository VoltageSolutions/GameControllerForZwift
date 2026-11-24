using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>
    /// Lays children out in equal columns, as many as fit when each column is at least <see cref="MinColumnWidth"/>
    /// wide, with <see cref="Gap"/> between columns and rows (docs/spec/ux/platforms.md). The Buttons list uses it to
    /// go from one column on a phone to two or three on a wide window, with no width checks in the screen.
    /// </summary>
    public class ColumnGrid : Panel
    {
        /// <summary>Identifies <see cref="MinColumnWidth"/>.</summary>
        public static readonly DependencyProperty MinColumnWidthProperty = DependencyProperty.Register(
            nameof(MinColumnWidth), typeof(double), typeof(ColumnGrid),
            new FrameworkPropertyMetadata(320.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Identifies <see cref="Gap"/>.</summary>
        public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
            nameof(Gap), typeof(double), typeof(ColumnGrid),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Gets or sets the least width of a column.</summary>
        public double MinColumnWidth
        {
            get => (double)GetValue(MinColumnWidthProperty);
            set => SetValue(MinColumnWidthProperty, value);
        }

        /// <summary>Gets or sets the space between columns and between rows.</summary>
        public double Gap
        {
            get => (double)GetValue(GapProperty);
            set => SetValue(GapProperty, value);
        }

        /// <summary>How many columns fit in a width. At least one.</summary>
        public static int ColumnCount(double width, double minColumnWidth, double gap) =>
            double.IsInfinity(width) ? 1 : Math.Max(1, (int)Math.Floor((width + gap) / (minColumnWidth + gap)));

        private List<UIElement> Shown => InternalChildren.Cast<UIElement>().Where(c => c.Visibility != Visibility.Collapsed).ToList();

        protected override Size MeasureOverride(Size availableSize)
        {
            int columns = ColumnCount(availableSize.Width, MinColumnWidth, Gap);
            double columnWidth = double.IsInfinity(availableSize.Width) ? MinColumnWidth : (availableSize.Width - ((columns - 1) * Gap)) / columns;
            List<UIElement> children = Shown;
            double height = 0;
            for (int start = 0; start < children.Count; start += columns)
            {
                double rowHeight = 0;
                for (int i = start; i < Math.Min(start + columns, children.Count); i++)
                {
                    children[i].Measure(new Size(columnWidth, double.PositiveInfinity));
                    rowHeight = Math.Max(rowHeight, children[i].DesiredSize.Height);
                }

                height += rowHeight + (start > 0 ? Gap : 0);
            }

            double width = double.IsInfinity(availableSize.Width) ? columnWidth : availableSize.Width;
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int columns = ColumnCount(finalSize.Width, MinColumnWidth, Gap);
            double columnWidth = (finalSize.Width - ((columns - 1) * Gap)) / columns;
            List<UIElement> children = Shown;
            double y = 0;
            for (int start = 0; start < children.Count; start += columns)
            {
                double rowHeight = 0;
                for (int i = start; i < Math.Min(start + columns, children.Count); i++)
                {
                    rowHeight = Math.Max(rowHeight, children[i].DesiredSize.Height);
                }

                for (int i = start; i < Math.Min(start + columns, children.Count); i++)
                {
                    children[i].Arrange(new Rect((i - start) * (columnWidth + Gap), y, columnWidth, rowHeight));
                }

                y += rowHeight + Gap;
            }

            return finalSize;
        }
    }
}
