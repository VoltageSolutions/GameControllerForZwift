using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

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

        private static string GetApplicationPath()
        {
            // Explicit override, e.g. for CI or a published build.
            var fromEnv = Environment.GetEnvironmentVariable("GC4Z_APP_PATH");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
                return Path.GetFullPath(fromEnv);

#if DEBUG
            const string configuration = "Debug";
#else
            const string configuration = "Release";
#endif
            // Walk up from the test output directory to the solution root, then look in the app's output folder.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GameControllerForZwift.sln")))
                dir = dir.Parent;
            if (dir == null)
                throw new FileNotFoundException("Could not locate GameControllerForZwift.sln above " + AppContext.BaseDirectory + "; set GC4Z_APP_PATH to the exe.");

            var binDir = Path.Combine(dir.FullName, "src", "Presentation", "GameControllerForZwift", "bin", configuration);
            var exe = Directory.Exists(binDir)
                ? Directory.GetFiles(binDir, "GameControllerForZwift.exe", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                : null;
            return exe ?? throw new FileNotFoundException("GameControllerForZwift.exe not found under " + binDir + "; build the app or set GC4Z_APP_PATH.");
        }

        public void Dispose()
        {
            System.Console.WriteLine("Closing GameControllerForZwift.exe");
            AppSession.Quit();
        }
    }
}
