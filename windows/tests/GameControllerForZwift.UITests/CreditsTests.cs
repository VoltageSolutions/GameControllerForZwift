namespace GameControllerForZwift.UITests
{
    [Collection("Application collection")]
    public sealed class CreditsTests : IDisposable
    {
        private readonly AppFixture _fixture;

        public CreditsTests(AppFixture fixture)
        {
            _fixture = fixture;
            _fixture.AppSession.Reset();
        }

        [Fact]
        public void NavigateToCredits()
        {
            _fixture.AppSession.OpenFromMenu("MenuCreditsItem");

            Assert.True(_fixture.AppSession.Exists("CreditsGitHubLink"));
            Assert.True(_fixture.AppSession.Exists("CreditsListLink"));
            Assert.True(_fixture.AppSession.Exists("CreditsLicenseLink"));
        }

        public void Dispose() => _fixture.AppSession.Reset();
    }
}
