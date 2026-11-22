using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Controls
{
    /// <summary>
    /// Lays out two children side by side when there is room and one above the other when there is not, as the tablet
    /// and phone layouts of a screen do (docs/spec/ux/platforms.md). The first child takes the remaining width. The
    /// second child has a fixed width beside the first.
    /// </summary>
    public class AdaptiveColumns : Panel
    {
        /// <summary>Identifies <see cref="SideWidth"/>.</summary>
        public static readonly DependencyProperty SideWidthProperty = DependencyProperty.Register(
            nameof(SideWidth), typeof(double), typeof(AdaptiveColumns),
            new FrameworkPropertyMetadata(520.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Identifies <see cref="Gap"/>.</summary>
        public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
            nameof(Gap), typeof(double), typeof(AdaptiveColumns),
            new FrameworkPropertyMetadata(48.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Identifies <see cref="StackBelow"/>.</summary>
        public static readonly DependencyProperty StackBelowProperty = DependencyProperty.Register(
            nameof(StackBelow), typeof(double), typeof(AdaptiveColumns),
            new FrameworkPropertyMetadata(900.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Gets or sets the width of the second column.</summary>
        public double SideWidth
        {
            get => (double)GetValue(SideWidthProperty);
            set => SetValue(SideWidthProperty, value);
        }

        /// <summary>Gets or sets the space between the columns, or between the stacked children.</summary>
        public double Gap
        {
            get => (double)GetValue(GapProperty);
            set => SetValue(GapProperty, value);
        }

        /// <summary>Gets or sets the width below which the children stack.</summary>
        public double StackBelow
        {
            get => (double)GetValue(StackBelowProperty);
            set => SetValue(StackBelowProperty, value);
        }

        private bool IsSideBySide(double width) => !double.IsInfinity(width) && width >= StackBelow && InternalChildren.Count == 2;

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = availableSize.Width;
            if (IsSideBySide(width))
            {
                UIElement main = InternalChildren[0];
                UIElement side = InternalChildren[1];
                side.Measure(new Size(SideWidth, double.PositiveInfinity));
                main.Measure(new Size(Math.Max(0, width - SideWidth - Gap), double.PositiveInfinity));
                return new Size(width, Math.Max(main.DesiredSize.Height, side.DesiredSize.Height));
            }

            double height = 0;
            double widest = 0;
            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(width, double.PositiveInfinity));
                height += child.DesiredSize.Height + (height > 0 ? Gap / 2 : 0);
                widest = Math.Max(widest, child.DesiredSize.Width);
            }

            return new Size(double.IsInfinity(width) ? widest : width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (IsSideBySide(finalSize.Width))
            {
                UIElement main = InternalChildren[0];
                UIElement side = InternalChildren[1];
                double mainWidth = Math.Max(0, finalSize.Width - SideWidth - Gap);
                main.Arrange(new Rect(0, 0, mainWidth, finalSize.Height));
                side.Arrange(new Rect(mainWidth + Gap, 0, SideWidth, Math.Min(finalSize.Height, side.DesiredSize.Height)));
                return finalSize;
            }

            double y = 0;
            foreach (UIElement child in InternalChildren)
            {
                child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height + Gap / 2;
            }

            return finalSize;
        }
    }
}
