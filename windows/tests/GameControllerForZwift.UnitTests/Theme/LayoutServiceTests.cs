using GameControllerForZwift.UI.WPF;
using GameControllerForZwift.UI.WPF.Theme;
using System.Windows;

namespace GameControllerForZwift.UnitTests.Theme
{
    /// <summary>The size class follows the navigation layout (UX-S-004 to UX-S-006).</summary>
    [Collection(WpfWindows.Name)]
    public class LayoutServiceTests
    {
        [Theory]
        [InlineData(NavLayout.BottomBar, SizeClass.Phone)]
        [InlineData(NavLayout.Rail, SizeClass.Tablet)]
        [InlineData(NavLayout.Sidebar, SizeClass.Desktop)]
        public void UXS005_TheSizeClassFollowsTheNavigationLayout(NavLayout layout, SizeClass expected)
        {
            Assert.Equal(expected, LayoutService.ForLayout(layout));
        }

        [Theory]
        [InlineData(NavLayout.BottomBar, SizeClass.Phone)]
        [InlineData(NavLayout.Rail, SizeClass.Tablet)]
        [InlineData(NavLayout.Sidebar, SizeClass.Desktop)]
        public void UXS004_EachNavigationLayoutLoadsItsLayoutDictionary(NavLayout layout, SizeClass sizeClass)
        {
            TestApplication.Run(() =>
            {
                var service = new LayoutService(Application.Current);
                try
                {
                    service.Apply(layout);

                    Assert.Equal(sizeClass, service.Current);
                    Assert.Equal(LayoutService.Create(sizeClass)["Layout.PagePadding"], Application.Current.Resources["Layout.PagePadding"]);
                    Assert.Equal(LayoutService.Create(sizeClass)["Layout.PageTitleSize"], Application.Current.Resources["Layout.PageTitleSize"]);
                }
                finally
                {
                    service.Apply(NavLayout.Sidebar);
                }
            });
        }

        [Fact]
        public void UXS006_ThePageTitleIs34OnPhoneAnd48Elsewhere()
        {
            TestApplication.Run(() =>
            {
                var service = new LayoutService(Application.Current);
                try
                {
                    service.Apply(NavLayout.BottomBar);
                    Assert.Equal(34.0, Application.Current.Resources["Layout.PageTitleSize"]);

                    service.Apply(NavLayout.Rail);
                    Assert.Equal(48.0, Application.Current.Resources["Layout.PageTitleSize"]);

                    service.Apply(NavLayout.Sidebar);
                    Assert.Equal(48.0, Application.Current.Resources["Layout.PageTitleSize"]);
                }
                finally
                {
                    service.Apply(NavLayout.Sidebar);
                }
            });
        }

        [Fact]
        public void UXS004_ApplyingTheSameLayoutTwiceKeepsOneDictionary()
        {
            TestApplication.Run(() =>
            {
                var service = new LayoutService(Application.Current);
                try
                {
                    service.Apply(NavLayout.Rail);
                    service.Apply(NavLayout.Rail);
                    service.Apply(NavLayout.BottomBar);

                    Assert.Single(Application.Current.Resources.MergedDictionaries, d => d.Contains("Layout.PagePadding"));
                }
                finally
                {
                    service.Apply(NavLayout.Sidebar);
                }
            });
        }

        [Fact]
        public void TheDictionariesLoadFromTheAppPackUris()
        {
            Assert.EndsWith("Resources/Layout.Phone.xaml", LayoutService.SourceFor(SizeClass.Phone).AbsolutePath, StringComparison.Ordinal);
            Assert.EndsWith("Resources/Layout.Desktop.xaml", LayoutService.SourceFor(SizeClass.Desktop).AbsolutePath, StringComparison.Ordinal);
        }
    }
}
