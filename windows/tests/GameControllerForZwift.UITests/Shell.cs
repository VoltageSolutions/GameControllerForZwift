using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace GameControllerForZwift.UITests
{
    /// <summary>Finds and drives the window shell: the tabs and the More menu (docs/spec/ux/navigation.md).</summary>
    internal static class Shell
    {
        public static AppiumElement Element(this WindowsDriver session, string automationId) =>
            session.FindElement(ByWindowsAutomation.AccessibilityId(automationId));

        public static bool Exists(this WindowsDriver session, string automationId) =>
            session.FindElements(ByWindowsAutomation.AccessibilityId(automationId)).Count > 0;

        public static bool IsSelected(this AppiumElement element) => element.Selected;

        /// <summary>Opens the More menu and picks an item.</summary>
        public static void OpenFromMenu(this WindowsDriver session, string itemId)
        {
            session.Element("MenuButton").Click();
            session.Element(itemId).Click();
        }

        /// <summary>Goes back to the Buttons tab at the default window size, so every test starts from the same place.</summary>
        public static void Reset(this WindowsDriver session)
        {
            if (session.Exists("MenuSettingsItem"))
            {
                session.Element("MenuButton").Click();
            }

            session.Manage().Window.Size = new System.Drawing.Size(1600, 800);
            session.Element("NavButtonsTab").Click();
        }

        /// <summary>
        /// Presses Escape in an element. WinAppDriver does not support W3C key actions, so the key goes through the element.
        /// </summary>
        public static void PressEscape(this WindowsDriver session, string automationId) =>
            session.Element(automationId).SendKeys(Keys.Escape);
    }
}
