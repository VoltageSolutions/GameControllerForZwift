using GameControllerForZwift.UI.WPF.Controls;
using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UnitTests.Controls
{
    /// <summary>The row button and the two-column panel (docs/spec/ux/visual-system.md, Rows and two-column screens).</summary>
    [Collection(WpfWindows.Name)]
    public class ActionRowTests
    {
        [Fact]
        public void UXV045_TheTitleIsTheContentSoScreenReadersReadItAsTheName()
        {
            TestApplication.Run(() =>
            {
                var row = new ActionRow { Title = "Open the log folder", Description = "Newest file", Trailing = ActionRowTrailing.External };

                Assert.Equal("Open the log folder", row.Content);
                Assert.Equal("Newest file", row.Description);
                Assert.Equal(ActionRowTrailing.External, row.Trailing);
            });
        }

        [Fact]
        public void UXV049_WideShowsTwoColumnsAndNarrowStacks()
        {
            TestApplication.Run(() =>
            {
                var first = new Border { Height = 100 };
                var second = new Border { Height = 200 };
                var panel = new AdaptiveColumns { SideWidth = 400, Gap = 40, StackBelow = 900 };
                panel.Children.Add(first);
                panel.Children.Add(second);

                panel.Measure(new Size(1000, double.PositiveInfinity));
                panel.Arrange(new Rect(0, 0, 1000, 200));
                Assert.Equal(200, panel.DesiredSize.Height);
                Assert.Equal(560, first.ActualWidth);
                Assert.Equal(400, second.ActualWidth);
                Assert.Equal(0, first.TranslatePoint(default, panel).X);
                Assert.Equal(600, second.TranslatePoint(default, panel).X);

                panel.Measure(new Size(600, double.PositiveInfinity));
                panel.Arrange(new Rect(0, 0, 600, panel.DesiredSize.Height));
                Assert.Equal(320, panel.DesiredSize.Height);
                Assert.Equal(600, second.ActualWidth);
                Assert.Equal(120, second.TranslatePoint(default, panel).Y);
            });
        }
    }
}
