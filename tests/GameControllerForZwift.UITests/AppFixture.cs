using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.UI;
using System.Runtime.InteropServices;

namespace GameControllerForZwift.UITests
{
    public class AppFixture : IDisposable
    {
        private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
        public WindowsDriver AppSession;

        public AppFixture()
        {
            // Set up the Windows Application Driver (WinAppDriver)
            var appCapabilities = new AppiumOptions
            {
                AutomationName = "windows",
                App = GetApplicationPath(),
                DeviceName = "WindowsPC",
                PlatformName = "Windows"
            };
            System.Console.WriteLine("Starting GameControllerForZwift.exe to run tests against.");
            AppSession = new WindowsDriver(new Uri(WindowsApplicationDriverUrl), appCapabilities);

            System.Console.WriteLine("Maximizing and restoring window to bring it to the foreground.");
            // Maximizing the window helps bring it to the foreground more effectively than user32 SetForegroundWindow
            AppSession.Manage().Window.Maximize();
            AppSession.Manage().Window.Size = new System.Drawing.Size(1600, 800);

            AppSession.GetScreenshot().SaveAsFile("screenshot.png");
        }

        private string GetApplicationPath()
        {
            // BaseDirectory points to the bin directory of the test project
            var testDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Navigate back to the solution root and then to the app's output directory
#if DEBUG
            string relativePathToApp = @"..\..\..\..\..\src\Presentation\GameControllerForZwift\bin\Debug\net9.0-windows10.0.17763.0\GameControllerForZwift.exe";
#else
            string relativePathToApp = @"..\..\..\..\..\src\Presentation\GameControllerForZwift\bin\Release\net9.0-windows10.0.17763.0\GameControllerForZwift.exe";
#endif
            return Path.GetFullPath(Path.Combine(testDirectory, relativePathToApp));
        }

        public void Dispose()
        {
            System.Console.WriteLine("Closing GameControllerForZwift.exe");
            AppSession.Quit();
        }
    }
}
