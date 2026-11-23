using GameControllerForZwift.UI.WPF.Converters;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameControllerForZwift.UnitTests.Theme
{
    /// <summary>The shared controls in Resources/Controls.xaml and Surfaces.xaml (docs/spec/ux/visual-system.md).</summary>
    [Collection(WpfWindows.Name)]
    public class ControlStylesTests
    {
        private static readonly string[] Choices = ["Default", "Climbing"];

        // Creates the element on the UI thread, shows it in a window, runs the test, and closes the window.
        private static void Hosted<T>(Func<T> create, Action<T> test) where T : FrameworkElement
        {
            TestApplication.Run(() =>
            {
                T element = create();
                var window = new Window
                {
                    Content = new StackPanel { Children = { element } },
                    Width = 900,
                    Height = 600,
                    ShowActivated = false,
                    ShowInTaskbar = false,
                };
                window.Show();
                window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
                try
                {
                    test(element);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static Color Token(string name) => (Color)Application.Current.Resources["Color." + name];

        private static Color ColorOf(Brush brush) => Assert.IsType<SolidColorBrush>(brush).Color;

        private static T Part<T>(Control control, string name) where T : class =>
            Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));

        private static Style Named(string key) => Assert.IsType<Style>(Application.Current.Resources[key]);

        [Fact]
        public void UXV016_UXV027_EveryButtonIsASecondaryPill()
        {
            Hosted(() => new Button { Content = "Cancel" }, button =>
            {
                Assert.Equal(48, button.ActualHeight);
                Assert.Equal(120, button.MinWidth);
                Assert.Equal(Token("surface-2"), ColorOf(button.Background));
                Assert.Equal(Token("text"), ColorOf(button.Foreground));
                Assert.Equal(button.ActualHeight / 2, Part<Border>(button, "Fill").CornerRadius.TopLeft);
            });
        }

        [Fact]
        public void UXV001_UXV002_PrimaryIsTheAccentPillWithADarkLabel()
        {
            Hosted(() => new Button { Content = "Start riding", Style = Named("Button.Primary") }, button =>
            {
                Assert.Equal(60, button.ActualHeight);
                Assert.True(button.ActualWidth >= 160);
                Assert.Equal(FontWeights.ExtraBold, button.FontWeight);
                Assert.Equal(Token("accent"), ColorOf(button.Background));
                Assert.Equal(Token("on-accent"), ColorOf(button.Foreground));
            });
        }

        [Fact]
        public void SCRBTN014_ADisabledButtonStaysVisibleWithoutTheAccent()
        {
            Hosted(() => new Button { Content = "Save", Style = Named("Button.Primary"), IsEnabled = false }, button =>
            {
                Assert.Equal(Visibility.Visible, button.Visibility);
                Assert.Equal(Token("surface-2"), ColorOf(Part<Border>(button, "Fill").Background));
                Assert.Equal(Token("muted"), ColorOf(button.Foreground));
            });
        }

        [Fact]
        public void UXV029_ALongLabelGrowsTheButton()
        {
            Hosted(() => new Button { Content = "Mit dem Fahren beginnen und weiterfahren", Style = Named("Button.Standard") }, button =>
            {
                Assert.Equal(56, button.ActualHeight);
                Assert.True(button.ActualWidth > 300, $"The button is {button.ActualWidth} wide.");
            });
        }

        [Fact]
        public void IconButtonIsA48Circle()
        {
            Hosted(() => new Button { Style = Named("Button.Icon") }, button =>
            {
                Assert.Equal(48, button.ActualWidth);
                Assert.Equal(48, button.ActualHeight);
            });
        }

        [Fact]
        public void UXV039_UXV040_DropdownCentersItsTextAndBoldsTheSelectedItem()
        {
            Hosted(() => new ComboBox { ItemsSource = Choices, SelectedIndex = 1, Width = 240 }, box =>
            {
                Assert.Equal(48, box.ActualHeight);
                Assert.Equal(VerticalAlignment.Center, Part<ContentPresenter>(box, "ContentSite").VerticalAlignment);
                Assert.Equal(Token("surface-2"), ColorOf(box.Background));

                box.IsDropDownOpen = true;
                box.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
                Assert.True(Part<Popup>(box, "PART_Popup").IsOpen);
                var selected = Assert.IsType<ComboBoxItem>(box.ItemContainerGenerator.ContainerFromIndex(1));
                Assert.Equal(FontWeights.Bold, selected.FontWeight);
                box.IsDropDownOpen = false;
            });
        }

        [Fact]
        public void UXV039_TextBoxIsA48Field()
        {
            Hosted(() => new TextBox { Text = "Climbing", Width = 240 }, box =>
            {
                Assert.Equal(48, box.ActualHeight);
                Assert.NotNull(box.Template.FindName("PART_ContentHost", box));
            });
        }

        [Fact]
        public void UXV041_ACheckedCheckboxUsesTheTextColor()
        {
            Hosted(() => new CheckBox { Content = "Show mappings from other controllers", IsChecked = true }, box =>
            {
                Assert.True(box.ActualHeight >= 48);
                Assert.Equal(Token("text"), ColorOf(Part<Border>(box, "Box").Background));
                Assert.Equal(Visibility.Visible, Part<FrameworkElement>(box, "Check").Visibility);
            });
        }

        [Fact]
        public void UXV041_ASwitchMovesItsKnobWhenOn()
        {
            Hosted(() => new CheckBox { Content = "Announce each shift", Style = Named("Switch") }, box =>
            {
                Assert.Equal(HorizontalAlignment.Left, Part<FrameworkElement>(box, "Knob").HorizontalAlignment);

                box.IsChecked = true;

                Assert.Equal(HorizontalAlignment.Right, Part<FrameworkElement>(box, "Knob").HorizontalAlignment);
                Assert.Equal(Token("text"), ColorOf(Part<Border>(box, "Track").Background));
            });
        }

        [Fact]
        public void UXV041_ASelectedRadioShowsItsDot()
        {
            Hosted(() => new RadioButton { Content = "Dark", IsChecked = true }, radio =>
            {
                Assert.Equal(Visibility.Visible, Part<FrameworkElement>(radio, "Dot").Visibility);
            });
        }

        [Theory]
        [InlineData("Surface.Card", 28)]
        [InlineData("Surface.Row", 20)]
        [InlineData("Surface.Panel", 32)]
        [InlineData("Surface.Chip", 999)]
        public void UXV017_UXV018_UXV019_SurfacesUseTheSpecRadii(string key, double radius)
        {
            Hosted(() => new Border { Style = Named(key) }, border =>
            {
                Assert.Equal(radius, border.CornerRadius.TopLeft);
                Assert.Equal(default, border.BorderThickness);
            });
        }

        [Fact]
        public void UXV012_UXV013_TypeRolesUseTheSpecSizesAndTabularFigures()
        {
            Hosted(() => new TextBlock { Text = "1,086", Style = Named("Text.Readout") }, text =>
            {
                Assert.Equal(64, text.FontSize);
                Assert.Equal(FontWeights.ExtraBold, text.FontWeight);
                Assert.Equal(FontNumeralAlignment.Tabular, System.Windows.Documents.Typography.GetNumeralAlignment(text));
            });
            Hosted(() => new TextBlock { Text = "Ready", Style = Named("Text.Display") }, text => Assert.Equal(52, text.FontSize));
        }

        [Fact]
        public void PillRadiusConverter_HalvesTheHeight()
        {
            var converter = new PillRadiusConverter();

            Assert.Equal(new CornerRadius(24), converter.Convert(48.0, typeof(CornerRadius), null!, CultureInfo.InvariantCulture));
            Assert.Equal(new CornerRadius(0), converter.Convert(double.PositiveInfinity, typeof(CornerRadius), null!, CultureInfo.InvariantCulture));
            Assert.Equal(new CornerRadius(0), converter.Convert("48", typeof(CornerRadius), null!, CultureInfo.InvariantCulture));
            Assert.Throws<NotSupportedException>(() => converter.ConvertBack(new CornerRadius(1), typeof(double), null!, CultureInfo.InvariantCulture));
        }
    }
}
