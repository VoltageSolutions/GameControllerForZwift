using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interfaces;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.UI;

namespace GameControllerForZwift.UITests
{
    public class UIModeTests
    {
        private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
        // where does this ID come from?
        private const string AppPath = @"C:\Users\elauber\repos\GameControllerForZwift\src\Presentation\GameControllerForZwift\bin\Debug\net9.0-windows10.0.17763.0\GameControllerForZwift.exe";
        private WindowsDriver _driver;

        public UIModeTests()
        {
            // Set up the Windows Application Driver (WinAppDriver)
            var windowsOptions = new AppiumOptions
            {
                AutomationName = "windows",
                PlatformName = "Windows",
                DeviceName = "WindowsPC",
                App = AppPath
            };

            _driver = new WindowsDriver(new Uri(WindowsApplicationDriverUrl), windowsOptions);
        }

        [Fact]
        public void TestSwitchToSettingsAndChangeMode()
        {
            // Arrange
            // Wait for the main window to load
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            // Find and click the Settings button (adjust locator as per your UI)
            var settingsButton = _driver.FindElement(By.Name("SettingsButtonId"));
            settingsButton.Click();

            //// Wait for the Settings window to load
            //wait.Until(d => d.FindElement(By.AccessibilityId("SettingsWindowId")));

            //// Find and toggle Light/Dark Mode switch
            //var modeToggleButton = _driver.FindElement(By.AccessibilityId("ModeToggleButtonId"));
            //modeToggleButton.Click();

            //// Verify if the mode has changed (you can check by inspecting the UI element or value)
            //var currentMode = _driver.FindElement(By.AccessibilityId("ModeTextId")).Text;
            //Assert.Contains("Dark", currentMode); // Assuming "Dark" appears when dark mode is active
        }

        public void Dispose()
        {
            // Clean up
            _driver.Quit();
        }
    }
}
