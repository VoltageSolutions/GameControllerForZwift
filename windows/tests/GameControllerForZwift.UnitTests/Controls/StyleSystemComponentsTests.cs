using GameControllerForZwift.UI.WPF.Controls;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameControllerForZwift.UnitTests.Controls
{
    /// <summary>The components of the style system (docs/spec/ux/style-system.md).</summary>
    [Collection(WpfWindows.Name)]
    public class StyleSystemComponentsTests
    {
        private static Border Box(double width = 10, double height = 10) => new() { Width = width, Height = height, HorizontalAlignment = HorizontalAlignment.Left };

        // Shows the element in a window, so the styles of the app apply to it as they do on a screen.
        private static void Hosted(FrameworkElement element, double width, Action test)
        {
            var host = new Grid { Width = width, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { element } };
            var window = new Window { Content = host, Width = width + 40, Height = 600, ShowActivated = false, ShowInTaskbar = false, Left = -20000, Top = -20000 };
            window.Show();
            window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, () => { });
            try
            {
                test();
            }
            finally
            {
                window.Close();
            }
        }

        private static void Layout(FrameworkElement element, double width, double height = double.PositiveInfinity)
        {
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, double.IsInfinity(width) ? element.DesiredSize.Width : width, element.DesiredSize.Height));
            element.UpdateLayout();
        }

        private static T Part<T>(Control control, string name) where T : class
        {
            control.ApplyTemplate();
            return Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
        }

        private static T? Descendant<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match || (match = Descendant<T>(child)!) is not null)
                {
                    return match;
                }
            }

            return null;
        }

        [Fact]
        public void UXV015_AVerticalStackPutsTheGapBetweenChildren()
        {
            TestApplication.Run(() =>
            {
                Border a = Box();
                Border b = Box();
                Border c = Box();
                var stack = new Stack { Gap = 8, Children = { a, b, c } };

                Layout(stack, 100);

                Assert.Equal(46, stack.DesiredSize.Height);
                Assert.Equal(new Point(0, 18), b.TranslatePoint(default, stack));
                Assert.Equal(new Point(0, 36), c.TranslatePoint(default, stack));
            });
        }

        [Fact]
        public void UXV015_ACollapsedChildTakesNoGap()
        {
            TestApplication.Run(() =>
            {
                Border hidden = Box();
                hidden.Visibility = Visibility.Collapsed;
                var stack = new Stack { Gap = 8, Children = { Box(), hidden, Box() } };

                Layout(stack, 100);

                Assert.Equal(28, stack.DesiredSize.Height);
            });
        }

        [Fact]
        public void UXV031_AHorizontalStackPutsTheGapBetweenChildren()
        {
            TestApplication.Run(() =>
            {
                Border second = Box(20);
                var stack = new Stack { Orientation = Orientation.Horizontal, Gap = 12, Children = { Box(20), second } };

                Layout(stack, 200);

                Assert.Equal(52, stack.DesiredSize.Width);
                Assert.Equal(new Point(32, 0), second.TranslatePoint(default, stack));
            });
        }

        [Fact]
        public void UXV031_AWrappingStackStartsANewRowWhenAChildDoesNotFit()
        {
            TestApplication.Run(() =>
            {
                Border third = Box(40);
                var stack = new Stack { Orientation = Orientation.Horizontal, Wrap = true, Gap = 10, Children = { Box(40), Box(40), third } };

                Layout(stack, 100);

                Assert.Equal(30, stack.DesiredSize.Height);
                Assert.Equal(new Point(0, 20), third.TranslatePoint(default, stack));
            });
        }

        [Fact]
        public void UXV031_AWrappedRowCentersItsShorterChildren()
        {
            TestApplication.Run(() =>
            {
                Border tall = Box(20, 40);
                Border short1 = Box(20, 10);
                short1.VerticalAlignment = VerticalAlignment.Center;
                var stack = new Stack { Orientation = Orientation.Horizontal, Wrap = true, Gap = 8, Children = { tall, short1 } };

                Layout(stack, 100);

                Assert.Equal(new Point(28, 15), short1.TranslatePoint(default, stack));
            });
        }

        [Theory]
        [InlineData(700, 320, 12, 2)]
        [InlineData(1000, 320, 12, 3)]
        [InlineData(300, 320, 12, 1)]
        [InlineData(double.PositiveInfinity, 320, 12, 1)]
        public void UXV049_AColumnGridFitsAsManyColumnsAsHaveRoom(double width, double minimum, double gap, int expected)
        {
            Assert.Equal(expected, ColumnGrid.ColumnCount(width, minimum, gap));
        }

        [Fact]
        public void UXV049_AColumnGridArrangesRowsInEqualColumns()
        {
            TestApplication.Run(() =>
            {
                Border third = new() { Height = 10 };
                var grid = new ColumnGrid { MinColumnWidth = 100, Gap = 10, Children = { new Border { Height = 10 }, new Border { Height = 10 }, third } };

                Layout(grid, 250);

                Assert.Equal(30, grid.DesiredSize.Height);
                Assert.Equal(new Point(0, 20), third.TranslatePoint(default, grid));
                Assert.Equal(120, grid.Children[0].RenderSize.Width);
            });
        }

        [Fact]
        public void UXV056_AStatusDotIsOkOrErr()
        {
            TestApplication.Run(() =>
            {
                var dot = new StatusDot();
                Hosted(dot, 100, () =>
                {
                    Assert.Equal(Application.Current.Resources["Brush.err"], Part<System.Windows.Shapes.Ellipse>(dot, "Dot").Fill);
                    Assert.Equal(8, Part<System.Windows.Shapes.Ellipse>(dot, "Dot").Width);

                    dot.IsOk = true;

                    Assert.Equal(Application.Current.Resources["Brush.ok"], Part<System.Windows.Shapes.Ellipse>(dot, "Dot").Fill);
                });
            });
        }

        [Fact]
        public void UXV056_AStatusChipIs40HighWithItsWords()
        {
            TestApplication.Run(() =>
            {
                var chip = new StatusChip { Text = "Controller connected", IsOk = true };
                Hosted(chip, 600, () =>
                {
                    Assert.Equal(40, chip.ActualHeight);
                    TextBlock words = Descendant<TextBlock>(chip)!;
                    Assert.Equal("Controller connected", words.Text);
                    Assert.Equal(Application.Current.Resources["Brush.text"], words.Foreground);
                });
            });
        }

        [Fact]
        public void UXV013_AStatShowsANumberOverALabel()
        {
            TestApplication.Run(() =>
            {
                var stat = new Stat { Value = "32 min", Label = "ride time" };
                Hosted(stat, 300, () =>
                {
                    var texts = new List<string>();
                    CollectText(stat, texts);
                    Assert.Equal(["32 min", "ride time"], texts);
                });
            });
        }

        [Fact]
        public void UXV047_ASectionShowsItsLabelAsAHeadingAndHidesAnEmptyOne()
        {
            TestApplication.Run(() =>
            {
                var section = new Section { Label = "Appearance", Content = Box() };
                var bare = new Section { Content = Box() };
                Hosted(new StackPanel { Children = { section, bare } }, 300, () =>
                {
                    var label = Part<TextBlock>(section, "LabelText");

                    Assert.Equal("Appearance", label.Text);
                    Assert.Equal(AutomationHeadingLevel.Level2, AutomationProperties.GetHeadingLevel(label));
                    Assert.Equal(Visibility.Visible, label.Visibility);
                    Assert.Equal(Visibility.Collapsed, Part<TextBlock>(bare, "LabelText").Visibility);
                });
            });
        }

        [Fact]
        public void UXV052_APageScrollsInsideItsPaddingAndSpansTheWidth()
        {
            TestApplication.Run(() =>
            {
                Border box = Box(10, 10);
                var page = new ScreenPage { Content = box };
                Hosted(page, 500, () =>
                {
                    ScrollViewer scroll = Descendant<ScrollViewer>(page)!;
                    Assert.Equal(ScrollBarVisibility.Disabled, scroll.HorizontalScrollBarVisibility);
                    Assert.Equal(500, scroll.ActualWidth);
                    var content = Assert.IsType<ContentPresenter>(VisualTreeHelper.GetParent(box));
                    Assert.Equal(Application.Current.Resources["Layout.PagePadding"], content.Margin);
                });
            });
        }

        private static void CollectText(DependencyObject root, List<string> texts)
        {
            if (root is TextBlock { Text.Length: > 0 } block)
            {
                texts.Add(block.Text);
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                CollectText(VisualTreeHelper.GetChild(root, i), texts);
            }
        }
    }
}
