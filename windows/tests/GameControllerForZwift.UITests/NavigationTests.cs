namespace GameControllerForZwift.UITests
{
    /// <summary>The tabs, the More menu, back, and where the tabs go for the window width.</summary>
    [Collection("Application collection")]
    public sealed class NavigationTests : IDisposable
    {
        private readonly AppFixture _fixture;

        public NavigationTests(AppFixture fixture)
        {
            _fixture = fixture;
            _fixture.AppSession.Reset();
        }

        [Fact]
        public void UXN001_EachTabShowsItsScreen()
        {
            var session = _fixture.AppSession;

            session.Element("NavRideTab").Click();
            Assert.True(session.Exists("RidePageTitle"));
            Assert.True(session.Element("NavRideTab").IsSelected());

            session.Element("NavConnectionsTab").Click();
            Assert.Equal("Connections", session.Element("ConnectionsPageTitle").Text);
            Assert.True(session.Exists("ConnectionsZwiftStatus"));

            session.Element("NavButtonsTab").Click();
            Assert.True(session.Exists("ButtonsPageTitle"));
            Assert.True(session.Element("NavButtonsTab").IsSelected());
        }

        [Fact]
        public void UXN008_MoreMenuOpensAndEscapeClosesIt()
        {
            var session = _fixture.AppSession;

            session.Element("MenuButton").Click();
            Assert.True(session.Exists("MenuSettingsItem"));
            Assert.True(session.Exists("MenuCreditsItem"));
            Assert.Equal("Close menu", session.Element("MenuButton").GetAttribute("Name"));

            session.PressEscape("MenuSettingsItem");

            Assert.False(session.Exists("MenuSettingsItem"));
        }

        [Fact]
        public void UXN009_UXN016_BackReturnsFromAPushedScreen()
        {
            var session = _fixture.AppSession;
            session.Element("NavConnectionsTab").Click();

            session.OpenFromMenu("MenuSettingsItem");
            Assert.False(session.Element("NavConnectionsTab").IsSelected());

            session.Element("NavBackButton").Click();

            Assert.True(session.Element("NavConnectionsTab").IsSelected());
            Assert.False(session.Exists("NavBackButton"));
        }

        [Theory]
        [InlineData(1400, "NavSidebar")]
        [InlineData(800, "NavRail")]
        [InlineData(520, "NavBottomBar")]
        public void UXN010_TabsMoveWithTheWindowWidth(int width, string navId)
        {
            var session = _fixture.AppSession;

            session.Manage().Window.Size = new System.Drawing.Size(width, 800);

            Assert.True(session.Exists(navId));
            Assert.True(session.Exists("NavRideTab"));
            Assert.True(session.Exists("MenuButton"));
        }

        [Fact]
        public void StyleGallery_ShowsInTheMenuOnlyInDebugBuilds()
        {
            var session = _fixture.AppSession;
            session.Element("MenuButton").Click();

#if DEBUG
            session.Element("MenuStyleGalleryItem").Click();
            Assert.Equal("Style gallery", session.Element("StyleGalleryPageTitle").Text);
            Assert.True(session.Exists("NavBackButton"));
#else
            Assert.False(session.Exists("MenuStyleGalleryItem"));
            session.PressEscape("MenuSettingsItem");
#endif
        }

        public void Dispose() => _fixture.AppSession.Reset();
    }
}
