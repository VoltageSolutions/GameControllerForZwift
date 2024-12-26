using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System.Diagnostics;

namespace GameControllerForZwift.UITests
{
    [Collection("Application collection")]
    public class SettingsTests : IClassFixture<AppFixture>, IDisposable
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
            var treeView = _fixture.AppSession.FindElement(ByWindowsAutomation.AccessibilityId("PagesTreeView"));
            //var treeItem = treeView.FindElement(By.XPath(".//TreeViewItem[.//TextBlock[contains(@Text, 'Controller Setup')]]"));



        }
    }
}
