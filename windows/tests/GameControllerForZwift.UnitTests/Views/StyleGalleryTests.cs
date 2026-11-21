using GameControllerForZwift.UI.WPF.Theme;
using GameControllerForZwift.UI.WPF.ViewModels;
using GameControllerForZwift.UI.WPF.Views;
using System.Windows.Controls;

namespace GameControllerForZwift.UnitTests.Views
{
    [Collection(WpfWindows.Name)]
    public class StyleGalleryTests
    {
        private static readonly string[] ChangedProperties = ["SelectedChoice", "IsChecked", "IsSwitchOn", "SampleText"];

        [Fact]
        public void Gallery_ShowsASwatchForEveryToken()
        {
            TestApplication.Run(() =>
            {
                var page = new StyleGalleryPage { DataContext = new StyleGalleryViewModel() };

                var swatches = Assert.IsType<WrapPanel>(page.FindName("Swatches"));
                Assert.Equal(ThemeService.TokenNames.Count, swatches.Children.Count);
                Assert.NotNull(page.FindName("SamplePrimaryButton"));
            });
        }

        [Fact]
        public void GalleryViewModel_HoldsTheSampleValues()
        {
            var viewModel = new StyleGalleryViewModel();
            var changed = new List<string?>();
            viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            Assert.Contains(viewModel.SelectedChoice, viewModel.SampleChoices);
            viewModel.SelectedChoice = "Default";
            viewModel.IsChecked = false;
            viewModel.IsSwitchOn = false;
            viewModel.SampleText = "Flat";

            Assert.Equal(ChangedProperties, changed);
        }
    }
}
