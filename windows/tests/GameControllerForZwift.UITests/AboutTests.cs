namespace GameControllerForZwift.UITests
{
    [Collection("Application collection")]
    public sealed class AboutTests : IDisposable
    {
        private readonly AppFixture _fixture;

        public AboutTests(AppFixture fixture)
        {
            _fixture = fixture;
            _fixture.AppSession.Reset();
        }

        [Fact]
        public void NavigateToCredits()
        {
            _fixture.AppSession.OpenFromMenu("MenuCreditsItem");

            Assert.True(_fixture.AppSession.Exists("OpenGitHubButton"));
            Assert.True(_fixture.AppSession.Exists("OpenkofiButton"));
        }

        public void Dispose() => _fixture.AppSession.Reset();
    }
}
