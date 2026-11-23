using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.Tests.ViewModels;
using GameControllerForZwift.UI.WPF.Theme;
using GameControllerForZwift.UI.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.Screenshots
{
    /// <summary>
    /// Renders every screen and state of the app to PNG files, in light and dark and at three window sizes, so screens
    /// can be compared with the mockups and the spec. It does nothing unless the environment variable
    /// <c>GCFZ_SCREENSHOTS</c> names an output folder (docs/build/build-and-test.md).
    /// </summary>
    [Collection(GameControllerForZwift.UnitTests.WpfWindows.Name)]
    [Trait("Category", "Screenshots")]
    public sealed class ScreenCatalogTests
    {
        private const string OutputVariable = "GCFZ_SCREENSHOTS";

        private sealed record WindowSize(string Name, int Width, int Height, string Layout);

        private static readonly WindowSize[] Sizes =
        [
            new("phone", 400, 860, "bottom bar"),
            new("tablet", 820, 1100, "rail"),
            new("desktop", 1280, 800, "sidebar"),
        ];

        private static readonly Appearance[] Themes = [Appearance.Light, Appearance.Dark];

        private sealed class ManualTime : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 9, 3, 7, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan by) => _now += by;
        }

        /// <summary>Everything one rendered window is built from. Scenarios drive the real view models through it.</summary>
        private sealed class Harness : IDisposable
        {
            private readonly ConfigurationTestContext _context = new();
            private readonly ServiceProvider _provider;
            private readonly ManualTime _time = new();

            public Harness(ThemeService theme, bool zwiftConnected, bool controllerConnected, Action<ConfigurationTestContext>? seed)
            {
                seed?.Invoke(_context);
                Connection.IsZwiftConnected.Returns(zwiftConnected);

                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton(Connection);
                services.AddSingleton(_context.Settings);
                services.AddSingleton<IAppSettingsStore>(_context.Settings);
                services.AddSingleton<IConfigurationStore>(_context.Store);
                services.AddSingleton<IDialogService>(_context.Dialogs);
                services.AddSingleton(theme);
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton(_ => new ZwiftStatus(Connection));
                services.AddSingleton(_ => controllerConnected ? _context.CreateViewModel(Controller(Xbox)) : _context.CreateViewModel());
                services.AddSingleton<ConnectionsViewModel>(sp => new ConnectionsViewModel(
                    sp.GetRequiredService<ZwiftStatus>(), sp.GetRequiredService<ControllerSetupViewModel>(), _ => { }));
                services.AddSingleton(sp => new RideViewModel(
                    sp.GetRequiredService<ZwiftStatus>(), sp.GetRequiredService<ControllerSetupViewModel>(), _context.Settings,
                    _context.Dialogs, sp.GetRequiredService<INavigationService>(), sp.GetRequiredService<ConnectionsViewModel>(), _time));
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<CreditsViewModel>();
                services.AddSingleton(_ => new CreditsListViewModel(ReadShippedFile("ACKNOWLEDGEMENTS.md"), _context.Dialogs));
                services.AddSingleton(_ => new LicenseViewModel(ReadShippedFile("LICENSE.md"), _context.Dialogs));
                services.AddSingleton<SupportViewModel>();
                services.AddSingleton(sp => new SettingsViewModel(
                    _context.Store, _context.Settings, _context.Dialogs, theme, sp.GetRequiredService<INavigationService>(),
                    Path.Join(Path.GetTempPath(), "GameControllerForZwift.Screenshots", "logs")));
                services.AddSingleton<StyleGalleryViewModel>();
                services.AddTransient<UI.WPF.MainWindow>();
                _provider = services.BuildServiceProvider();
            }

            public IZwiftConnection Connection { get; } = Substitute.For<IZwiftConnection>();

            public ConfigurationTestContext Context => _context;

            public UI.WPF.MainWindow Window { get; private set; } = null!;

            public RideViewModel Ride => _provider.GetRequiredService<RideViewModel>();

            public ControllerSetupViewModel Setup => _provider.GetRequiredService<ControllerSetupViewModel>();

            public MainWindowViewModel Shell => _provider.GetRequiredService<MainWindowViewModel>();

            public CreditsViewModel Credits => _provider.GetRequiredService<CreditsViewModel>();

            public void Open(WindowSize size)
            {
                Window = _provider.GetRequiredService<UI.WPF.MainWindow>();
                Window.ShowActivated = false;
                Window.ShowInTaskbar = false;
                Window.WindowStartupLocation = WindowStartupLocation.Manual;
                Window.Left = -20000;
                Window.Top = -20000;
                Window.Width = size.Width;
                Window.Height = size.Height;
                Window.Show();
                Pump();
            }

            public void Pump()
            {
                Dispatcher dispatcher = Window?.Dispatcher ?? Dispatcher.CurrentDispatcher;
                dispatcher.Invoke(DispatcherPriority.Background, () => { });
                for (int i = 0; i < 6; i++)
                {
                    dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
                }
            }

            public void SetZwift(bool connected)
            {
                Connection.ConnectionChanged += Raise.Event<EventHandler<bool>>(Connection, connected);
                Pump();
            }

            public void Press(ControllerInput input)
            {
                var data = new ControllerData();
                switch (input)
                {
                    case ControllerInput.X:
                        data.X = true;
                        break;
                    case ControllerInput.A:
                        data.A = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(input));
                }

                Setup.CurrentControllerValues = data;
                Setup.CurrentControllerValues = new ControllerData();
            }

            public void MapXToShiftUp() =>
                Setup.ButtonFunctionMappings.First(m => m.SelectedInput == ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.ShiftUp;

            public void AdvanceAndCheckTimeout(TimeSpan by)
            {
                _time.Advance(by);
                Ride.CheckTimeout();
            }

            public void Dispose()
            {
                Window?.Close();
                _provider.Dispose();
                _context.Dispose();
            }
        }

        /// <summary>One screen in one state, and how to reach it through the real view models.</summary>
        private sealed record Scenario(string Screen, string State, string Title, bool ZwiftConnected, bool ControllerConnected, Action<Harness> Arrange,
            Func<Harness, string?>? Check = null);

        private static readonly Scenario[] Scenarios =
        [
            new("ride", "home", "Ride, home, both connected", true, true, h =>
            {
                // Both connected would start a ride, so Home with both connected needs the ride to have ended and been dismissed.
                h.AdvanceAndCheckTimeout(TimeSpan.FromMinutes(11));
                h.Ride.DoneCommand.Execute(null);
            }, h => h.Ride.IsHome ? null : "Home with both connected is not reachable: a ride starts when both are connected"),
            new("ride", "nocontroller", "Ride, no controller", true, false, _ => { }, h => h.Ride.IsHome ? null : "not on Home"),
            new("ride", "zwiftnotconnected", "Ride, Zwift not connected", false, true, _ => { }, h => h.Ride.IsHome ? null : "not on Home"),
            new("ride", "riding", "Ride, riding, last press Shift up", true, true, h =>
            {
                h.MapXToShiftUp();
                h.Press(ControllerInput.X);
            }, h => h.Ride.IsRiding && h.Ride.HasLastPress ? null : "the riding view or last press did not show"),
            new("ride", "zwiftlost", "Ride, riding, Zwift lost", true, true, h =>
            {
                h.MapXToShiftUp();
                h.Press(ControllerInput.X);
                h.SetZwift(false);
            }, h => h.Ride.IsRiding && h.Ride.HasProblem ? null : "the riding view or the Zwift problem did not show"),
            new("ride", "controllerlost", "Ride, riding, controller disconnected", true, true, h =>
            {
                h.MapXToShiftUp();
                h.Press(ControllerInput.X);
                h.Setup.SelectedController = null;
            }, h => h.Ride.IsRiding && h.Ride.HasProblem ? null : "the riding view or the controller problem did not show"),
            new("ride", "finished-ask", "Ride finished, with the donation ask", true, true, h => FinishRide(h), h => h.Ride.IsFinished && h.Ride.ShowDonationAsk ? null : "the summary or the ask did not show"),
            new("ride", "finished-noask", "Ride finished, without the donation ask", true, true, h => FinishRide(h), h => h.Ride.IsFinished && !h.Ride.ShowDonationAsk ? null : "the summary did not show, or the ask showed"),
            new("buttons", "list", "Buttons, mapping list", true, true, h => h.Shell.SelectTab(ShellTab.Buttons), null),
            new("buttons", "picker", "Buttons, row selected with the function picker open", true, true, h =>
            {
                h.Shell.SelectTab(ShellTab.Buttons);
                h.Pump();
                h.Setup.SelectRowCommand.Execute(h.Setup.Rows.Single(r => r.Input == ControllerInput.X));
            }, h => h.Setup.FunctionPicker.IsOpen ? null : "the function picker did not open"),
            new("connections", "connected", "Connections, Zwift connected", true, true, h => h.Shell.SelectTab(ShellTab.Connections), null),
            new("connections", "notconnected", "Connections, Zwift not connected, steps shown", false, true, h => h.Shell.SelectTab(ShellTab.Connections),
                h => h.Shell.CurrentViewModel is ConnectionsViewModel { ShowSteps: true } ? null : "the steps did not show"),
            new("settings", "default", "Settings", true, true, h => h.Shell.OpenSettingsCommand.Execute(null), null),
            new("about", "default", "About", true, true, h => h.Shell.OpenCreditsCommand.Execute(null), null),
            new("credits", "list", "About, Credits list", true, true, h =>
            {
                h.Shell.OpenCreditsCommand.Execute(null);
                h.Pump();
                h.Credits.OpenCreditsListCommand.Execute(null);
            }, null),
            new("license", "default", "About, License", true, true, h =>
            {
                h.Shell.OpenCreditsCommand.Execute(null);
                h.Pump();
                h.Credits.OpenLicenseCommand.Execute(null);
            }, null),
            new("support", "default", "Support this project", true, true, h => h.Shell.OpenSupportCommand.Execute(null), null),
            new("more", "menu", "More menu open, over Ride", true, true, h =>
            {
                h.AdvanceAndCheckTimeout(TimeSpan.FromMinutes(11));
                h.Ride.DoneCommand.Execute(null);
                h.Shell.ToggleMoreMenuCommand.Execute(null);
            }, h => h.Shell.IsMoreMenuOpen ? null : "the menu did not open"),
        ];

        private static readonly (string Screen, string State, string Reason)[] SkippedStates =
        [
            ("help", "default", "Help and feedback has no screen yet: its More menu item is disabled (MainWindowViewModel.OpenHelp), so no view model reaches it."),
        ];

        private static void FinishRide(Harness h)
        {
            h.MapXToShiftUp();

            // Presses every few minutes keep the ride going, so it ends once, after the last one.
            for (int i = 0; i < 5; i++)
            {
                h.Press(ControllerInput.X);
                h.AdvanceAndCheckTimeout(TimeSpan.FromMinutes(8));
            }

            h.AdvanceAndCheckTimeout(TimeSpan.FromMinutes(3));
        }

        [Fact]
        public void RenderScreenCatalog()
        {
            string? folder = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            Directory.CreateDirectory(folder);
            GameControllerForZwift.UnitTests.TestApplication.Run(() => Render(Path.GetFullPath(folder)));
        }

        private static void Render(string folder)
        {
            Application app = Application.Current;
            ThemeMode originalMode = app.ThemeMode;
            var originalDictionaries = app.Resources.MergedDictionaries.ToList();
            var files = new List<string>();
            var skipped = SkippedStates.Select(s => $"{s.Screen}-{s.State}: {s.Reason}").ToList();
            var images = new Dictionary<string, BitmapSource>();
            var reasons = new Dictionary<string, string>();

            try
            {
                foreach (Appearance appearance in Themes)
                {
                    using var theme = new ThemeService();
                    theme.Apply(appearance);
                    string themeName = appearance.ToString().ToLowerInvariant();

                    foreach (Scenario scenario in Scenarios)
                    {
                        string key = $"{scenario.Screen}-{scenario.State}";
                        foreach (WindowSize size in Sizes)
                        {
                            using var harness = new Harness(theme, scenario.ZwiftConnected, scenario.ControllerConnected, null);
                            if (scenario.State == "finished-noask")
                            {
                                harness.Context.Settings.SetDonationAskShown();
                            }

                            harness.Open(size);
                            scenario.Arrange(harness);
                            harness.Pump();

                            string? problem = scenario.Check?.Invoke(harness);
                            if (problem is not null)
                            {
                                reasons[key] = problem;
                                continue;
                            }

                            string name = $"{key}-{themeName}-{size.Name}";
                            BitmapSource image = Capture(harness.Window);
                            Save(image, Path.Join(folder, name + ".png"));
                            files.Add(name + ".png");
                            images[name] = image;

                            double extra = ScrollableHeight(harness.Window);
                            if (extra > 1)
                            {
                                harness.Window.Height = size.Height + extra;
                                harness.Pump();
                                BitmapSource full = Capture(harness.Window);
                                Save(full, Path.Join(folder, name + "-full.png"));
                                files.Add(name + "-full.png");
                            }
                        }
                    }
                }

                var sheets = new List<string>();
                foreach (Scenario scenario in Scenarios)
                {
                    string key = $"{scenario.Screen}-{scenario.State}";
                    if (reasons.TryGetValue(key, out string? reason))
                    {
                        skipped.Add($"{key}: {reason}");
                        continue;
                    }

                    string sheet = $"sheet-{key}.png";
                    Save(ContactSheet(scenario, images), Path.Join(folder, sheet));
                    sheets.Add(sheet);
                }

                WriteIndex(folder, files, sheets, skipped);
            }
            finally
            {
                // Leave the application as the other tests expect it: the theme mode and the dictionaries it started with.
                app.ThemeMode = originalMode;
                app.Resources.MergedDictionaries.Clear();
                foreach (ResourceDictionary dictionary in originalDictionaries)
                {
                    app.Resources.MergedDictionaries.Add(dictionary);
                }
            }
        }

        private static RenderTargetBitmap Capture(Window window)
        {
            window.UpdateLayout();
            int width = (int)Math.Ceiling(window.ActualWidth);
            int height = (int)Math.Ceiling(window.ActualHeight);
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            bitmap.Freeze();
            return bitmap;
        }

        private static void Save(BitmapSource image, string path)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using FileStream stream = File.Create(path);
            encoder.Save(stream);
        }

        // How much taller the window must be to show a scrolling page in full: the largest overflow of its scroll viewers.
        private static double ScrollableHeight(UI.WPF.MainWindow window)
        {
            double most = 0;
            if (window.FindName("RootContent") is DependencyObject root)
            {
                Visit(root);
            }

            return most;

            void Visit(DependencyObject node)
            {
                if (node is ScrollViewer { IsVisible: true } viewer)
                {
                    most = Math.Max(most, viewer.ScrollableHeight);
                }

                int count = VisualTreeHelper.GetChildrenCount(node);
                for (int i = 0; i < count; i++)
                {
                    Visit(VisualTreeHelper.GetChild(node, i));
                }
            }
        }

        private static RenderTargetBitmap ContactSheet(Scenario scenario, Dictionary<string, BitmapSource> images)
        {
            const double cellWidth = 480;
            const double cellHeight = 640;
            const double gap = 16;
            const double labelHeight = 28;
            double width = (Sizes.Length * (cellWidth + gap)) + gap;
            double height = gap + labelHeight + (Themes.Length * (cellHeight + labelHeight + gap));
            var sheet = new DrawingVisual();
            using (DrawingContext context = sheet.RenderOpen())
            {
                context.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)), null, new Rect(0, 0, width, height));
                DrawLabel(context, scenario.Title, gap, gap / 2, 18);
                for (int row = 0; row < Themes.Length; row++)
                {
                    string theme = Themes[row].ToString().ToLowerInvariant();
                    for (int column = 0; column < Sizes.Length; column++)
                    {
                        WindowSize size = Sizes[column];
                        double x = gap + (column * (cellWidth + gap));
                        double y = gap + labelHeight + (row * (cellHeight + labelHeight + gap));
                        DrawLabel(context, $"{theme} / {size.Name} {size.Width}x{size.Height} ({size.Layout})", x, y, 14);
                        if (images.TryGetValue($"{scenario.Screen}-{scenario.State}-{theme}-{size.Name}", out BitmapSource? image))
                        {
                            double scale = Math.Min(cellWidth / image.PixelWidth, cellHeight / image.PixelHeight);
                            context.DrawImage(image, new Rect(x, y + labelHeight, image.PixelWidth * scale, image.PixelHeight * scale));
                        }
                    }
                }
            }

            var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(sheet);
            bitmap.Freeze();
            return bitmap;
        }

        private static void DrawLabel(DrawingContext context, string text, double x, double y, double size)
        {
            var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), size, Brushes.White, 1.0);
            context.DrawText(formatted, new Point(x, y));
        }

        private static void WriteIndex(string folder, List<string> files, List<string> sheets, List<string> skipped)
        {
            var text = new StringBuilder();
            text.AppendLine("# Screen catalog");
            text.AppendLine();
            text.AppendLine("## Contact sheets");
            text.AppendLine();
            foreach (string sheet in sheets)
            {
                text.AppendLine(CultureInfo.InvariantCulture, $"- [{sheet}]({sheet})");
            }

            text.AppendLine();
            text.AppendLine("## Images");
            text.AppendLine();
            foreach (string file in files.Order(StringComparer.Ordinal))
            {
                text.AppendLine(CultureInfo.InvariantCulture, $"- [{file}]({file})");
            }

            text.AppendLine();
            text.AppendLine("## Skipped states");
            text.AppendLine();
            if (skipped.Count == 0)
            {
                text.AppendLine("None.");
            }

            foreach (string line in skipped)
            {
                text.AppendLine(CultureInfo.InvariantCulture, $"- {line}");
            }

            File.WriteAllText(Path.Join(folder, "index.md"), text.ToString());
        }

        // The shipped markdown files sit next to the app. A test run may not copy them, so the repo root is the fallback.
        private static string ReadShippedFile(string name)
        {
            for (string? directory = AppContext.BaseDirectory; directory is not null; directory = Path.GetDirectoryName(directory))
            {
                string path = Path.Join(directory, name);
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
            }

            return string.Empty;
        }
    }
}
