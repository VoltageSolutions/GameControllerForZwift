using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Controls;
using GameControllerForZwift.UI.WPF.Tests.ViewModels;
using GameControllerForZwift.UI.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UnitTests.Controls
{
    /// <summary>The controller illustration on the Buttons screen (SCR-BTN-004, SCR-BTN-006, and the image role in the accessibility spec).</summary>
    [Collection(WpfWindows.Name)]
    public class ControllerDiagramViewTests
    {
        private const double Full = 32767;

        private static void WithDiagram(Action<ControllerDiagramView> test) => WithDiagram(ControllerKind.Generic, test);

        private static void WithDiagram(ControllerKind kind, Action<ControllerDiagramView> test)
        {
            TestApplication.Run(() =>
            {
                var diagram = new ControllerDiagramView { Kind = kind };
                var window = new Window { Content = diagram, Width = 300, Height = 260, ShowActivated = false, ShowInTaskbar = false };
                window.Show();
                try
                {
                    test(diagram);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static Brush FillOf(Shape? shape) => shape!.Fill;

        private static readonly ControllerInput[] CommonInputs =
        [
            ControllerInput.A, ControllerInput.B, ControllerInput.X, ControllerInput.Y, ControllerInput.View, ControllerInput.Menu,
            ControllerInput.DPadUp, ControllerInput.DPadDown, ControllerInput.DPadLeft, ControllerInput.DPadRight,
            ControllerInput.LeftBumper, ControllerInput.RightBumper, ControllerInput.LeftTrigger, ControllerInput.RightTrigger,
            ControllerInput.LeftThumbstickClick, ControllerInput.RightThumbstickClick,
        ];

        /// <summary>The inputs each kind draws (SCR-BTN-048): the Xbox and DualShock 4 drawings have no bumpers or triggers; every other kind shows the Generic drawing.</summary>
        private static ControllerInput[] InputsOf(ControllerKind kind) => kind switch
        {
            // The Switch Pro drawing has bumpers (its shoulder arcs) and no triggers.
            ControllerKind.SwitchPro => [.. CommonInputs.Where(i => i is not (ControllerInput.LeftTrigger or ControllerInput.RightTrigger))],
            ControllerKind.Xbox or ControllerKind.PlayStationDualShock4 => [.. CommonInputs.Where(i => i is not (ControllerInput.LeftBumper or ControllerInput.RightBumper or ControllerInput.LeftTrigger or ControllerInput.RightTrigger))],
            _ => CommonInputs,
        };

        /// <summary>Kinds drawn with the Generic drawing have analog trigger fills; the Xbox and DualShock 4 drawings have no triggers.</summary>
        private static bool IsAnalog(ControllerKind kind) => kind is not (ControllerKind.Xbox or ControllerKind.PlayStationDualShock4 or ControllerKind.SwitchPro);

        public static TheoryData<ControllerKind> AllKinds => [.. Enum.GetValues<ControllerKind>()];

        [Theory]
        [InlineData(0, 0)]
        [InlineData(Full, ControllerDiagramView.MaxStickTravel)]
        [InlineData(-Full, -ControllerDiagramView.MaxStickTravel)]
        [InlineData(Full * 10, ControllerDiagramView.MaxStickTravel)]
        [InlineData(-Full * 10, -ControllerDiagramView.MaxStickTravel)]
        public void StickOffset_ScalesAndClampsToTheTravel(double axis, double expected) =>
            Assert.Equal(expected, ControllerDiagramView.StickOffset(axis), 6);

        [Theory]
        [InlineData(-5, 0)]
        [InlineData(0, 0)]
        [InlineData(Full, 1)]
        [InlineData(Full * 2, 1)]
        public void TriggerRatio_IsClampedToZeroThroughOne(double value, double expected) =>
            Assert.Equal(expected, ControllerDiagramView.TriggerRatio(value), 6);

        [Fact]
        public void UXV063_TriggerClip_RisesFromTheBottomOfTheFill()
        {
            var bounds = new Rect(120, 1, 32, 47.5);

            Rect empty = ControllerDiagramView.TriggerClip(bounds, 0);
            Rect full = ControllerDiagramView.TriggerClip(bounds, 1);
            Rect half = ControllerDiagramView.TriggerClip(bounds, 0.5);

            Assert.Equal(0, empty.Height);
            Assert.Equal(bounds.Bottom, empty.Bottom, 6);
            Assert.Equal(bounds.Top, full.Top, 6);
            Assert.Equal(bounds.Bottom, full.Bottom, 6);
            Assert.Equal(bounds.Height / 2, half.Height, 6);
            Assert.Equal(bounds.X, half.X);
            Assert.Equal(bounds.Width, half.Width);
        }

        [Fact]
        public void TriggerClip_ClampsTheRatio()
        {
            var bounds = new Rect(0, 0, 10, 20);

            Assert.Equal(bounds.Top, ControllerDiagramView.TriggerClip(bounds, 5).Top, 6);
            Assert.Equal(0, ControllerDiagramView.TriggerClip(bounds, -1).Height);
        }

        [Fact]
        public void Refresh_AtRestShowsEveryKeyUnlitAndTheSticksCentered()
        {
            WithDiagram(diagram =>
            {
                diagram.RefreshDiagram();

                Assert.Equal(0, diagram.LeftStickTransform!.X);
                Assert.Equal(0, diagram.RightStickTransform!.Y);
                Assert.Equal(0, diagram.LeftTriggerClipRect!.Value.Height);
                Assert.Equal(0, diagram.RightTriggerClipRect!.Value.Height);
                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.InputShape(ControllerInput.A)));
            });
        }

        [Fact]
        public void SCRBTN004_TheSelectedInputIsLit()
        {
            WithDiagram(diagram =>
            {
                diagram.SelectedInput = ControllerInput.RightBumper;
                diagram.RefreshDiagram();

                Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.InputShape(ControllerInput.RightBumper)));
                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.InputShape(ControllerInput.LeftBumper)));

                diagram.SelectedInput = null;
                diagram.RefreshDiagram();

                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.InputShape(ControllerInput.RightBumper)));
            });
        }

        [Fact]
        public void SCRBTN006_APressedRowLightsItsInputAndReleasingClearsIt()
        {
            using var context = new ConfigurationTestContext();
            WithDiagram(diagram =>
            {
                var viewModel = context.CreateViewModel(Xbox);
                diagram.Rows = viewModel.Rows;

                viewModel.Selector(ControllerInput.A).ControllerData = new ControllerData { A = true };
                diagram.RefreshDiagram();

                Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.InputShape(ControllerInput.A)));
                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.InputShape(ControllerInput.B)));

                viewModel.Selector(ControllerInput.A).ControllerData = new ControllerData();
                diagram.RefreshDiagram();

                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.InputShape(ControllerInput.A)));
            });
        }

        [Fact]
        public void ControllerValues_MoveTheSticksAndFillTheTriggers()
        {
            WithDiagram(diagram =>
            {
                diagram.ControllerValues = new ControllerData
                {
                    LeftThumbstickX = Full,
                    LeftThumbstickY = -Full,
                    RightThumbstickX = -Full,
                    RightThumbstickY = Full,
                    LeftTrigger = Full,
                    RightTrigger = Full / 2,
                };
                diagram.RefreshDiagram();

                Assert.Equal(ControllerDiagramView.MaxStickTravel, diagram.LeftStickTransform!.X, 6);
                Assert.Equal(-ControllerDiagramView.MaxStickTravel, diagram.LeftStickTransform!.Y, 6);
                Assert.Equal(-ControllerDiagramView.MaxStickTravel, diagram.RightStickTransform!.X, 6);
                Assert.Equal(ControllerDiagramView.MaxStickTravel, diagram.RightStickTransform!.Y, 6);
                Assert.True(diagram.LeftTriggerClipRect!.Value.Height > diagram.RightTriggerClipRect!.Value.Height);
                Assert.True(diagram.RightTriggerClipRect!.Value.Height > 0);

                diagram.ControllerValues = null;
                diagram.RefreshDiagram();

                Assert.Equal(0, diagram.LeftStickTransform!.X);
                Assert.Equal(0, diagram.LeftTriggerClipRect!.Value.Height);
            });
        }

        [Fact]
        public void ChangingTheStateRedrawsOnTheDispatcherWithoutAnExplicitRefresh()
        {
            WithDiagram(diagram =>
            {
                diagram.ControllerValues = new ControllerData { LeftThumbstickX = Full };
                diagram.SelectedInput = ControllerInput.A;
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, () => { });

                Assert.Equal(ControllerDiagramView.MaxStickTravel, diagram.LeftStickTransform!.X, 6);
                Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.InputShape(ControllerInput.A)));
            });
        }

        [Fact]
        public void RowsCollectionChanges_AreFollowed()
        {
            using var context = new ConfigurationTestContext();
            TestApplication.Run(() =>
            {
                var viewModel = context.CreateViewModel(Xbox);
                ButtonRowViewModel row = viewModel.Rows.Single(r => r.Input == ControllerInput.B);
                var rows = new ObservableCollection<ButtonRowViewModel>();
                var diagram = new ControllerDiagramView { Rows = rows };
                var window = new Window { Content = diagram, ShowActivated = false, ShowInTaskbar = false };
                window.Show();
                try
                {
                    rows.Add(row);
                    viewModel.Selector(ControllerInput.B).ControllerData = new ControllerData { B = true };
                    diagram.RefreshDiagram();
                    Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.InputShape(ControllerInput.B)));

                    rows.Remove(row);
                    diagram.RefreshDiagram();
                    Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.InputShape(ControllerInput.B)));

                    rows.Add(row);
                    rows.Clear();
                    diagram.RefreshDiagram();
                    Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.InputShape(ControllerInput.B)));
                }
                finally
                {
                    window.Close();
                }

            });
        }

        [Fact]
        public void TheDiagramIsAnImageNamedYourController()
        {
            WithDiagram(diagram =>
            {
                diagram.SetValue(AutomationProperties.AutomationIdProperty, "ButtonsControllerImage");
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(diagram);

                Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
                Assert.Equal("Your controller", peer.GetName());
                Assert.Equal("ButtonsControllerImage", peer.GetAutomationId());
                Assert.Equal(nameof(ControllerDiagramView), peer.GetClassName());
            });
        }

        [Fact]
        public void AnExplicitAutomationNameWins()
        {
            WithDiagram(diagram =>
            {
                diagram.SetValue(AutomationProperties.NameProperty, "Controller picture");

                Assert.Equal("Controller picture", UIElementAutomationPeer.CreatePeerForElement(diagram).GetName());
            });
        }

        [Theory]
        [MemberData(nameof(AllKinds))]
        public void UXV059_UXV067_EveryKindLoadsItsSvgWithTheIdsItShouldHave(ControllerKind kind)
        {
            WithDiagram(kind, diagram =>
            {
                foreach (ControllerInput input in InputsOf(kind))
                    Assert.True(diagram.InputShape(input) is not null, $"{kind} has no shape with id {input}.");

                Assert.NotNull(diagram.LeftStickTransform);
                Assert.NotNull(diagram.RightStickTransform);
            });
        }

        [Theory]
        [MemberData(nameof(AllKinds))]
        public void SCRBTN048_AKindDrawsOnlyTheInputsItHas(ControllerKind kind)
        {
            WithDiagram(kind, diagram =>
            {
                var drawn = Enum.GetValues<ControllerInput>().Where(i => diagram.InputShape(i) is not null).ToHashSet();

                Assert.Subset(InputsOf(kind).ToHashSet(), drawn);
                Assert.Superset(InputsOf(kind).ToHashSet(), drawn);
            });
        }

        [Theory]
        [MemberData(nameof(AllKinds))]
        public void UXV063_UXV064_OnlyAnalogTriggersHaveAFill(ControllerKind kind)
        {
            WithDiagram(kind, diagram =>
            {
                bool left = InputsOf(kind).Contains(ControllerInput.LeftTrigger);
                bool right = InputsOf(kind).Contains(ControllerInput.RightTrigger);

                Assert.Equal(IsAnalog(kind) && left, diagram.Element("LeftTriggerFill") is not null);
                Assert.Equal(IsAnalog(kind) && right, diagram.Element("RightTriggerFill") is not null);

                diagram.ControllerValues = new ControllerData { LeftTrigger = Full, RightTrigger = Full / 2 };
                diagram.RefreshDiagram();

                if (IsAnalog(kind))
                {
                    Assert.True(diagram.LeftTriggerClipRect!.Value.Height > diagram.RightTriggerClipRect!.Value.Height);
                    Assert.True(diagram.RightTriggerClipRect!.Value.Height > 0);
                }
                else
                {
                    Assert.Null(diagram.LeftTriggerClipRect);
                    Assert.Null(diagram.RightTriggerClipRect);
                }
            });
        }

        [Theory]
        [MemberData(nameof(AllKinds))]
        public void UXV060_EveryKindLightsTheSelectedAndPressedInputsAndKeepsTheirSize(ControllerKind kind)
        {
            using var context = new ConfigurationTestContext();
            WithDiagram(kind, diagram =>
            {
                var viewModel = context.CreateViewModel(Xbox);
                diagram.Rows = viewModel.Rows;
                ControllerInput selected = InputsOf(kind)[0];
                Rect before = diagram.InputShape(selected)!.RenderedGeometry.Bounds;
                object accent = diagram.FindResource("Brush.accent");

                diagram.SelectedInput = selected;
                diagram.RefreshDiagram();
                Assert.Same(accent, FillOf(diagram.InputShape(selected)));
                Assert.Equal(before, diagram.InputShape(selected)!.RenderedGeometry.Bounds);
                foreach (ControllerInput other in InputsOf(kind).Skip(1))
                    Assert.Same(diagram.FindResource(RestKeyFor(kind, other)), FillOf(diagram.InputShape(other)));

                diagram.SelectedInput = null;
                ControllerInput pressed = InputsOf(kind)[^1];
                var row = viewModel.Rows.FirstOrDefault(r => r.Input == pressed);
                if (row is not null)
                {
                    viewModel.Selector(pressed).ControllerData = ControllerDataFor(pressed);
                    diagram.RefreshDiagram();
                    Assert.Same(accent, FillOf(diagram.InputShape(pressed)));
                }
            });
        }

        // The flat DualShock 4 art rests keys and stick caps at Brush.art-shade; the other kinds use Brush.pad-key.
        private static string RestKeyFor(ControllerKind kind, ControllerInput input) =>
            kind is ControllerKind.Xbox or ControllerKind.PlayStationDualShock4 or ControllerKind.SwitchPro ? "Brush.art-shade" : "Brush.pad-key";

        // Stick travel is per drawing: the DualShock 4 art moves its caps 10 units, the others the default.
        private static double TravelOf(ControllerKind kind) =>
            kind is ControllerKind.Xbox or ControllerKind.PlayStationDualShock4 or ControllerKind.SwitchPro ? 10.0 : ControllerDiagramView.MaxStickTravel;

        private static ControllerData ControllerDataFor(ControllerInput input) => input switch
        {
            ControllerInput.Menu => new ControllerData { Menu = true },
            ControllerInput.View => new ControllerData { View = true },
            ControllerInput.RightThumbstickClick => new ControllerData { RightThumbstickClick = true },
            ControllerInput.LeftThumbstickClick => new ControllerData { LeftThumbstickClick = true },
            ControllerInput.RightTrigger => new ControllerData { RightTrigger = Full },
            _ => new ControllerData(),
        };

        [Theory]
        [MemberData(nameof(AllKinds))]
        public void UXV060_EveryKindMovesItsSticksByTheAxis(ControllerKind kind)
        {
            WithDiagram(kind, diagram =>
            {
                diagram.ControllerValues = new ControllerData { LeftThumbstickX = Full, LeftThumbstickY = -Full, RightThumbstickX = -Full, RightThumbstickY = Full };
                diagram.RefreshDiagram();

                if (diagram.LeftStickTransform is { } left)
                {
                    Assert.Equal(TravelOf(kind), left.X, 6);
                    Assert.Equal(-TravelOf(kind), left.Y, 6);
                }
                if (diagram.RightStickTransform is { } right)
                {
                    Assert.Equal(-TravelOf(kind), right.X, 6);
                    Assert.Equal(TravelOf(kind), right.Y, 6);
                }
            });
        }

        [Fact]
        public void UXV060_FreeKeysAreDashed()
        {
            TestApplication.Run(() =>
            {
                var drawing = ControllerSvgLoader.Load(System.Xml.Linq.XDocument.Parse(
                    "<svg viewBox=\"0 0 10 10\"><style>.free{stroke-dasharray:4 3}</style><g id=\"SL\"><rect class=\"free\" x=\"0\" y=\"0\" width=\"5\" height=\"5\"/></g></svg>"));
                var sl = Assert.IsType<System.Windows.Controls.Canvas>(drawing.ById["SL"]);
                var shape = Assert.IsAssignableFrom<Shape>(sl.Children[0]);

                Assert.NotEmpty(shape.StrokeDashArray);
            });
        }

        [Fact]
        public void SCRBTN046_ChangingTheKindSwapsTheDrawingAndKeepsTheState()
        {
            WithDiagram(ControllerKind.Generic, diagram =>
            {
                diagram.SelectedInput = ControllerInput.A;
                diagram.RefreshDiagram();
                Shape? genericA = diagram.InputShape(ControllerInput.A);

                diagram.Kind = ControllerKind.PlayStationDualShock4;
                diagram.RefreshDiagram();

                Assert.NotSame(genericA, diagram.InputShape(ControllerInput.A));
                Assert.Null(diagram.Element("LeftTriggerFill"));
                Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.InputShape(ControllerInput.A)));
            });
        }

        [Fact]
        public void SCRBTN047_TheAccessibleNameFollowsTheKind()
        {
            WithDiagram(diagram =>
            {
                var names = new HashSet<string>();
                foreach (ControllerKind kind in Enum.GetValues<ControllerKind>())
                {
                    diagram.Kind = kind;
                    string name = UIElementAutomationPeer.CreatePeerForElement(diagram).GetName();
                    Assert.Equal(ControllerKindNames.GetAccessibleName(kind), name);
                    names.Add(name);
                }

                Assert.Equal(Enum.GetValues<ControllerKind>().Length, names.Count);
            });
        }

        [Fact]
        public void SwitchPro_DPadArmsHaveNoStrokeAtRestAndAStrokeWhenLit()
        {
            WithDiagram(ControllerKind.SwitchPro, diagram =>
            {
                diagram.RefreshDiagram();
                Assert.Null(diagram.InputShape(ControllerInput.DPadUp)!.Stroke);

                diagram.SelectedInput = ControllerInput.DPadUp;
                diagram.RefreshDiagram();
                Assert.NotNull(diagram.InputShape(ControllerInput.DPadUp)!.Stroke);
                Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.InputShape(ControllerInput.DPadUp)));
            });
        }

        [Fact]
        public void UXV067_KindsWithoutTheirOwnDrawingUseTheGenericDrawing()
        {
            ControllerKind[] own = [ControllerKind.Generic, ControllerKind.Xbox, ControllerKind.PlayStationDualShock4, ControllerKind.SwitchPro];
            foreach (ControllerKind kind in Enum.GetValues<ControllerKind>().Except(own))
                Assert.Equal(ControllerSvgLoader.RelativePath(ControllerKind.Generic), ControllerSvgLoader.RelativePath(kind));

            Assert.NotEqual(ControllerSvgLoader.RelativePath(ControllerKind.Generic), ControllerSvgLoader.RelativePath(ControllerKind.PlayStationDualShock4));
            Assert.NotEqual(ControllerSvgLoader.RelativePath(ControllerKind.Generic), ControllerSvgLoader.RelativePath(ControllerKind.Xbox));
            Assert.NotEqual(ControllerSvgLoader.RelativePath(ControllerKind.Generic), ControllerSvgLoader.RelativePath(ControllerKind.SwitchPro));
        }

        [Fact]
        public void UXV067_EverySvgInTheDocsFolderHasAKindAndTheLoaderReadsIt()
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null && !Directory.Exists(Path.Combine(dir, "docs", "assets", "controllers")))
                dir = Path.GetDirectoryName(dir);
            Assert.NotNull(dir);

            string root = Path.Combine(dir!, "docs", "assets", "controllers");
            foreach (ControllerKind kind in Enum.GetValues<ControllerKind>())
            {
                string file = Path.Combine(root, ControllerSvgLoader.RelativePath(kind).Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(file), file);

                TestApplication.Run(() =>
                {
                    var drawing = ControllerSvgLoader.Load(System.Xml.Linq.XDocument.Load(file));
                    Assert.True(drawing.Root.Width > 0 && drawing.Root.Height > 0);
                });
            }
        }
    }
}
