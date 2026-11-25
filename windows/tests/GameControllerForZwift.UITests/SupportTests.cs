namespace GameControllerForZwift.UITests
{
    [Collection("Application collection")]
    public sealed class SupportTests : IDisposable
    {
        private readonly AppFixture _fixture;

        public SupportTests(AppFixture fixture)
        {
            _fixture = fixture;
            _fixture.AppSession.Reset();
        }

        [Fact]
        public void NavigateToSupport()
        {
            _fixture.AppSession.OpenFromMenu("MenuSupportItem");

            Assert.True(_fixture.AppSession.Exists("SupportKofiButton"));
        }

        public void Dispose() => _fixture.AppSession.Reset();
    }
}
