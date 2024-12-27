using OpenQA.Selenium.Appium;

namespace GameControllerForZwift.UITests
{
    [Collection("Application collection")]
    public class AboutTests : IClassFixture<AppFixture>, IDisposable
    {
        private AppFixture _fixture;

        public AboutTests(AppFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void NavigateToAbout()
        {
            // Arrange
            var aboutTreeViewItem = _fixture.AppSession.FindElement(ByWindowsAutomation.AccessibilityId("About"));

            // Act
            aboutTreeViewItem.Click();

            var githubButton= _fixture.AppSession.FindElement(ByWindowsAutomation.AccessibilityId("OpenGitHubButton"));
            var kofiButton = _fixture.AppSession.FindElement(ByWindowsAutomation.AccessibilityId("OpenkofiButton"));

            // Assert
            Assert.NotNull(githubButton);
            Assert.NotNull(kofiButton);
        }

        public void Dispose()
        {
            // Return to default state
            var controllerSetupTreeViewItem = _fixture.AppSession.FindElement(ByWindowsAutomation.AccessibilityId("Controller Setup"));
            controllerSetupTreeViewItem.Click();
        }
    }
}
