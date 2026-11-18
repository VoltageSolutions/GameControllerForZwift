using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Theme;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;
using System.Windows;

namespace GameControllerForZwift.UnitTests.Theme
{
    [Collection(WpfWindows.Name)]
    public class SettingsAppearanceTests
    {
        private static readonly string[] ExpectedLabels = ["Light", "Dark", "Match Windows"];

        private static SettingsViewModel CreateViewModel(ThemeService? theme) => new(
            Substitute.For<IConfigurationStore>(), Substitute.For<IAppSettingsStore>(), Substitute.For<IDialogService>(), theme);

        [Fact]
        public void SCRSET001_OptionsAreLightDarkAndMatchWindows()
        {
            Assert.Equal(ExpectedLabels, SettingsViewModel.AppearanceOptions.Select(o => o.Label));
            Assert.Equal(new[] { Appearance.Light, Appearance.Dark, Appearance.MatchWindows }, SettingsViewModel.AppearanceOptions.Select(o => o.Value));
        }

        [Fact]
        public void SCRSET001_DefaultsToMatchWindowsWithoutAThemeService()
        {
            SettingsViewModel viewModel = CreateViewModel(null);

            Assert.Equal(Appearance.MatchWindows, viewModel.Appearance);
            viewModel.Appearance = Appearance.Dark;
            Assert.Equal(Appearance.Dark, viewModel.Appearance);
        }

        [Fact]
        public void SCRSET001_ChoosingAnAppearanceAppliesIt()
        {
            TestApplication.Run(() =>
            {
                var modes = new List<ThemeMode>();
                using var theme = new ThemeService(Application.Current, () => false, () => false, modes.Add);
                try
                {
                    SettingsViewModel viewModel = CreateViewModel(theme);
                    Assert.Equal(Appearance.MatchWindows, viewModel.Appearance);

                    viewModel.Appearance = Appearance.Light;

                    Assert.Equal(Appearance.Light, theme.Appearance);
                    Assert.Equal(TokenSet.Light, theme.CurrentTokens);
                    Assert.Equal(ThemeMode.Light, Assert.Single(modes));
                }
                finally
                {
                    theme.Apply(Appearance.Dark);
                }
            });
        }
    }
}
