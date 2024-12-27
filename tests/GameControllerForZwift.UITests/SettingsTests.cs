using OpenQA.Selenium.Appium;

namespace GameControllerForZwift.UITests
{
    [Collection("Application collection")]
    public class SettingsTests : IDisposable
    {
        private AppFixture _fixture;

        public SettingsTests(AppFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void NavigateToSettings()
        {
            // Arrange
            var winButton = _fixture.AppSession.FindElement(ByWindowsAutomation.AccessibilityId("SettingsButton"));

            // Act
            winButton.Click();

            var title = _fixture.AppSession.FindElement(ByWindowsAutomation.AccessibilityId("PageTitle"));
            var titleText = title.Text;

            // Assert
            Assert.Equal("Appearance & behavior", titleText);
        }

        public void Dispose()
        {
            // Return to default state
            var controllerSetupTreeViewItem = _fixture.AppSession.FindElement(ByWindowsAutomation.AccessibilityId("Controller Setup"));
            controllerSetupTreeViewItem.Click();
        }
    }
}
