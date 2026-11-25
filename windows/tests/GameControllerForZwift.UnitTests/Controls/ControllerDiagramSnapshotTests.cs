using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Controls;
using GameControllerForZwift.UI.WPF.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;

namespace GameControllerForZwift.UnitTests.Controls
{
    /// <summary>Renders the real controller illustration to PNG files for visual review. Does nothing unless GCZ_SNAPSHOTS or a .snapshots/enable file opts in.</summary>
    [Collection(WpfWindows.Name)]
    public class ControllerDiagramSnapshotTests
    {
        private const double Full = 32767;
        private static readonly string[] Themes = ["Dark", "Light"];
        private static readonly string[] States = ["rest", "lit", "active"];

        private static string? OutputFolder()
        {
            string? fromEnvironment = Environment.GetEnvironmentVariable("GCZ_SNAPSHOTS");
            if (!string.IsNullOrEmpty(fromEnvironment))
                return fromEnvironment;

            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string folder = Path.Combine(dir.FullName, ".snapshots");
                if (File.Exists(Path.Combine(folder, "enable")))
                    return folder;
            }
            return null;
        }

        private static List<ButtonRowViewModel> LitRows()
        {
            ControllerInput[] inputs = [ControllerInput.A, ControllerInput.LeftBumper, ControllerInput.DPadUp];
            return [.. inputs.Select(input =>
            {
                var selector = new ZwiftFunctionSelectorViewModel { SelectedInput = input };
                selector.ControllerData = new ControllerData { A = true, LeftBumper = true, DPadUp = true };
                return new ButtonRowViewModel(new ButtonsInputInfo(input, input.ToString(), input.ToString(), input.ToString(), default), selector);
            })];
        }

        private static void Save(ControllerDiagramView diagram, Brush background, string path)
        {
            var host = new Border { Background = background, Width = 600, Height = 500, Child = diagram };
            host.Measure(new Size(600, 500));
            host.Arrange(new Rect(0, 0, 600, 500));
            host.UpdateLayout();
            diagram.RefreshDiagram();
            host.UpdateLayout();
            var bitmap = new RenderTargetBitmap(1200, 1000, 192, 192, PixelFormats.Pbgra32);
            bitmap.Render(host);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(path);
            encoder.Save(stream);
            host.Child = null;
        }

        [Fact]
        public void RenderEveryKindToPng()
        {
            string? folder = OutputFolder();
            if (folder is null)
                return;
            Directory.CreateDirectory(folder);

            TestApplication.Run(() =>
            {
                foreach (string theme in Themes)
                {
                    var tokens = new ResourceDictionary { Source = new Uri($"pack://application:,,,/GameControllerForZwift;component/Resources/Theme/Tokens.{theme}.xaml") };
                    foreach (ControllerKind kind in Enum.GetValues<ControllerKind>())
                    {
                        string stem = Path.Combine(folder, $"{kind}-{theme.ToLowerInvariant()}");
                        foreach (string state in States)
                        {
                            var diagram = new ControllerDiagramView { Kind = kind };
                            diagram.Resources.MergedDictionaries.Add(tokens);
                            if (state == "lit")
                                diagram.Rows = LitRows();
                            if (state == "active")
                            {
                                diagram.ControllerValues = new ControllerData
                                {
                                    LeftThumbstickX = 0.8 * Full,
                                    LeftThumbstickY = -0.5 * Full,
                                    RightThumbstickX = -0.9 * Full,
                                    RightThumbstickY = 0.3 * Full,
                                    LeftTrigger = 0.6 * Full,
                                    RightTrigger = 0.6 * Full,
                                    A = true,
                                    LeftBumper = true,
                                };
                            }
                            Save(diagram, (Brush)tokens["Brush.bg"], $"{stem}-{state}.png");
                        }
                    }
                }
            });
        }
    }
}
