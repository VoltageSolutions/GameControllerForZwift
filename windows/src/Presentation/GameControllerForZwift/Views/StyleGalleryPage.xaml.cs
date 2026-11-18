using GameControllerForZwift.UI.WPF.Theme;
using System.Windows;
using System.Windows.Controls;

namespace GameControllerForZwift.UI.WPF.Views
{
    /// <summary>The style gallery, a Debug-only screen that shows every shared component in each state.</summary>
    public partial class StyleGalleryPage : UserControl
    {
        public StyleGalleryPage()
        {
            InitializeComponent();
            AddSwatches();
        }

        // One swatch per color token. Each swatch follows the token, so switching the theme recolors it.
        private void AddSwatches()
        {
            foreach (string token in ThemeService.TokenNames)
            {
                var color = new Border { Width = 120, Height = 64, CornerRadius = new CornerRadius(16) };
                color.SetResourceReference(Border.BackgroundProperty, "Brush." + token);

                var name = new TextBlock { Margin = new Thickness(0, 8, 0, 0), Text = token };
                name.SetResourceReference(StyleProperty, "Text.Caption");

                var swatch = new StackPanel { Margin = new Thickness(0, 0, 16, 16), Width = 120 };
                swatch.Children.Add(color);
                swatch.Children.Add(name);
                System.Windows.Automation.AutomationProperties.SetName(swatch, token);
                Swatches.Children.Add(swatch);
            }
        }
    }
}
