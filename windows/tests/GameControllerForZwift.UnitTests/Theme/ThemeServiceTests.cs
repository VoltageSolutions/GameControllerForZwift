using GameControllerForZwift.UI.WPF.Theme;
using System.Windows;
using System.Windows.Media;

namespace GameControllerForZwift.UnitTests.Theme
{
    [Collection(WpfWindows.Name)]
    public class ThemeServiceTests
    {
        [Theory]
        [InlineData(Appearance.Light, false, false, TokenSet.Light)]
        [InlineData(Appearance.Light, true, false, TokenSet.Light)]
        [InlineData(Appearance.Dark, true, false, TokenSet.Dark)]
        [InlineData(Appearance.MatchWindows, true, false, TokenSet.Light)]
        [InlineData(Appearance.MatchWindows, false, false, TokenSet.Dark)]
        [InlineData(Appearance.Light, true, true, TokenSet.HighContrast)]
        [InlineData(Appearance.Dark, false, true, TokenSet.HighContrast)]
        public void SCRSET001_Resolve_PicksTheTokenSet(Appearance appearance, bool systemLight, bool highContrast, TokenSet expected)
            => Assert.Equal(expected, ThemeService.Resolve(appearance, systemLight, highContrast));

        [Fact]
        public void ToThemeMode_MatchesTheAppearance()
        {
            Assert.Equal(ThemeMode.Light, ThemeService.ToThemeMode(Appearance.Light));
            Assert.Equal(ThemeMode.Dark, ThemeService.ToThemeMode(Appearance.Dark));
            Assert.Equal(ThemeMode.System, ThemeService.ToThemeMode(Appearance.MatchWindows));
        }

        [Fact]
        public void Constructor_RejectsNulls()
        {
            TestApplication.Run(() =>
            {
                Assert.Throws<ArgumentNullException>(() => new ThemeService(null!, () => true, () => false, null));
                Assert.Throws<ArgumentNullException>(() => new ThemeService(Application.Current, null!, () => false, null));
                Assert.Throws<ArgumentNullException>(() => new ThemeService(Application.Current, () => true, null!, null));
            });
        }

        [Theory]
        [InlineData(TokenSet.Light, "#FAF8F4")]
        [InlineData(TokenSet.Dark, "#141312")]
        public void CreateTokens_LoadsTheXamlTokens(TokenSet tokens, string background)
        {
            TestApplication.Run(() =>
            {
                ResourceDictionary dictionary = ThemeService.CreateTokens(tokens);

                Assert.Equal((Color)ColorConverter.ConvertFromString(background), (Color)dictionary["Color.bg"]);
                Assert.IsType<SolidColorBrush>(dictionary["Brush.accent"]);
                Assert.Equal(Colors.Transparent, ((SolidColorBrush)dictionary["CardStrokeColorDefaultBrush"]).Color);
            });
        }

        [Fact]
        public void CreateTokens_HighContrastUsesSystemColors()
        {
            TestApplication.Run(() =>
            {
                ResourceDictionary dictionary = ThemeService.CreateTokens(TokenSet.HighContrast);

                Assert.Equal(SystemColors.WindowColor, (Color)dictionary["Color.bg"]);
                Assert.Equal(SystemColors.WindowTextColor, ((SolidColorBrush)dictionary["Brush.text"]).Color);
                Assert.Equal(SystemColors.HighlightTextColor, (Color)dictionary["Color.on-accent"]);
            });
        }

        [Fact]
        public void Apply_PutsOneTokenDictionaryLastAndFollowsTheAppearance()
        {
            TestApplication.Run(() =>
            {
                Application app = Application.Current;
                var modes = new List<ThemeMode>();
                bool systemLight = false;
                using var service = new ThemeService(app, () => systemLight, () => false, modes.Add);
                int changes = 0;
                service.TokensChanged += (_, _) => changes++;
                try
                {
                    service.Apply(Appearance.Light);

                    Assert.Equal(Appearance.Light, service.Appearance);
                    Assert.Equal(TokenSet.Light, service.CurrentTokens);
                    Assert.Equal(ThemeMode.Light, Assert.Single(modes));
                    AssertTokensLast(app, "#FAF8F4");

                    service.Apply(Appearance.MatchWindows);
                    Assert.Equal(TokenSet.Dark, service.CurrentTokens);
                    AssertTokensLast(app, "#141312");

                    systemLight = true;
                    service.Refresh();
                    Assert.Equal(TokenSet.Light, service.CurrentTokens);

                    int before = changes;
                    service.Refresh();
                    Assert.Equal(before, changes);
                    AssertTokensLast(app, "#FAF8F4");
                    Assert.Equal(3, changes);
                    Assert.Equal(new[] { ThemeMode.Light, ThemeMode.System }, modes);
                }
                finally
                {
                    service.Apply(Appearance.Dark);
                }
            });
        }

        [Fact]
        public void Apply_HighContrastWinsOverTheAppearance()
        {
            TestApplication.Run(() =>
            {
                Application app = Application.Current;
                bool highContrast = true;
                using var service = new ThemeService(app, () => true, () => highContrast, _ => { });
                try
                {
                    service.Apply(Appearance.Light);

                    Assert.Equal(TokenSet.HighContrast, service.CurrentTokens);
                    Assert.Equal(SystemColors.WindowColor, (Color)app.Resources["Color.bg"]);

                    highContrast = false;
                    service.Refresh();
                    Assert.Equal(TokenSet.Light, service.CurrentTokens);
                }
                finally
                {
                    service.Apply(Appearance.Dark);
                }
            });
        }

        [Fact]
        public void UXV011_FigtreeShipsEveryWeightTheSpecUses()
        {
            TestApplication.Run(() =>
            {
                var family = (FontFamily)Application.Current.Resources["Font.Figtree"];
                Assert.Equal(family.Source, ((FontFamily)Application.Current.Resources["ContentControlThemeFontFamily"]).Source);

                ICollection<FontFamily> families = Fonts.GetFontFamilies(
                    new Uri("pack://application:,,,/GameControllerForZwift;component/Assets/Fonts/"));
                FontFamily figtree = Assert.Single(families, f => f.FamilyNames.Values.Contains("Figtree"));
                HashSet<int> weights = figtree.FamilyTypefaces.Select(t => t.Weight.ToOpenTypeWeight()).ToHashSet();

                Assert.Superset(new HashSet<int> { 400, 500, 600, 700, 800 }, weights);
            });
        }

        private static void AssertTokensLast(Application app, string background)
        {
            var merged = app.Resources.MergedDictionaries;
            Assert.Single(merged, d => d.Contains("Color.bg"));
            Assert.True(merged[^1].Contains("Color.bg"));
            Assert.Equal((Color)ColorConverter.ConvertFromString(background), (Color)app.Resources["Color.bg"]);
        }
    }
}
