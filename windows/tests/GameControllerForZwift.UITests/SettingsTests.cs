namespace GameControllerForZwift.UITests
{
    [Collection("Application collection")]
    public sealed class SettingsTests : IDisposable
    {
        private readonly AppFixture _fixture;

        public SettingsTests(AppFixture fixture)
        {
            _fixture = fixture;
            _fixture.AppSession.Reset();
        }

        [Fact]
        public void NavigateToSettings()
        {
            _fixture.AppSession.OpenFromMenu("MenuSettingsItem");

            Assert.Equal("Appearance & behavior", _fixture.AppSession.Element("PageTitle").Text);
            Assert.True(_fixture.AppSession.Exists("NavBackButton"));
        }

        public void Dispose() => _fixture.AppSession.Reset();
    }
}
