using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Controls;
using GameControllerForZwift.UI.WPF.Tests.ViewModels;
using GameControllerForZwift.UI.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Shapes;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UnitTests.Controls
{
    /// <summary>The controller illustration on the Buttons screen (SCR-BTN-004, SCR-BTN-006, and the image role in the accessibility spec).</summary>
    [Collection(WpfWindows.Name)]
    public class ControllerDiagramViewTests
    {
        private const double Full = 32767;

        private static void WithDiagram(Action<ControllerDiagramView> test)
        {
            TestApplication.Run(() =>
            {
                var diagram = new ControllerDiagramView();
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

        private static Brush FillOf(Shape shape) => shape.Fill;

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
        public void TriggerClip_RisesFromTheBottomOfThePill()
        {
            var seed = new Rect(120, 48, 32, 0);

            Rect empty = ControllerDiagramView.TriggerClip(seed, 0);
            Rect full = ControllerDiagramView.TriggerClip(seed, 1);
            Rect half = ControllerDiagramView.TriggerClip(seed, 0.5);

            Assert.Equal(0, empty.Height);
            Assert.Equal(ControllerDiagramView.TriggerBottomY, empty.Bottom, 6);
            Assert.Equal(ControllerDiagramView.TriggerTopY, full.Top, 6);
            Assert.Equal(ControllerDiagramView.TriggerBottomY, full.Bottom, 6);
            Assert.Equal((ControllerDiagramView.TriggerBottomY - ControllerDiagramView.TriggerTopY) / 2, half.Height, 6);
            Assert.Equal(seed.X, half.X);
            Assert.Equal(seed.Width, half.Width);
        }

        [Fact]
        public void TriggerClip_ClampsTheRatio()
        {
            Rect over = ControllerDiagramView.TriggerClip(new Rect(0, 0, 10, 0), 5);
            Rect under = ControllerDiagramView.TriggerClip(new Rect(0, 0, 10, 0), -1);

            Assert.Equal(ControllerDiagramView.TriggerTopY, over.Top, 6);
            Assert.Equal(0, under.Height);
        }

        [Fact]
        public void Refresh_AtRestShowsEveryKeyUnlitAndTheSticksCentered()
        {
            WithDiagram(diagram =>
            {
                diagram.RefreshDiagram();

                Assert.Equal(0, diagram.LeftStickTransform.X);
                Assert.Equal(0, diagram.RightStickTransform.Y);
                Assert.Equal(0, diagram.LeftTriggerClip.Rect.Height);
                Assert.Equal(0, diagram.RightTriggerClip.Rect.Height);
                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.ButtonAHighlight));
            });
        }

        [Fact]
        public void SCRBTN004_TheSelectedInputIsLit()
        {
            WithDiagram(diagram =>
            {
                diagram.SelectedInput = ControllerInput.RightBumper;
                diagram.RefreshDiagram();

                Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.RightBumperHighlight));
                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.LeftBumperHighlight));

                diagram.SelectedInput = null;
                diagram.RefreshDiagram();

                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.RightBumperHighlight));
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

                Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.ButtonAHighlight));
                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.ButtonBHighlight));

                viewModel.Selector(ControllerInput.A).ControllerData = new ControllerData();
                diagram.RefreshDiagram();

                Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.ButtonAHighlight));
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

                Assert.Equal(ControllerDiagramView.MaxStickTravel, diagram.LeftStickTransform.X, 6);
                Assert.Equal(-ControllerDiagramView.MaxStickTravel, diagram.LeftStickTransform.Y, 6);
                Assert.Equal(-ControllerDiagramView.MaxStickTravel, diagram.RightStickTransform.X, 6);
                Assert.Equal(ControllerDiagramView.MaxStickTravel, diagram.RightStickTransform.Y, 6);
                Assert.True(diagram.LeftTriggerClip.Rect.Height > diagram.RightTriggerClip.Rect.Height);
                Assert.True(diagram.RightTriggerClip.Rect.Height > 0);

                diagram.ControllerValues = null;
                diagram.RefreshDiagram();

                Assert.Equal(0, diagram.LeftStickTransform.X);
                Assert.Equal(0, diagram.LeftTriggerClip.Rect.Height);
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

                Assert.Equal(ControllerDiagramView.MaxStickTravel, diagram.LeftStickTransform.X, 6);
                Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.ButtonAHighlight));
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
                    Assert.Same(diagram.FindResource("Brush.accent"), FillOf(diagram.ButtonBHighlight));

                    rows.Remove(row);
                    diagram.RefreshDiagram();
                    Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.ButtonBHighlight));

                    rows.Add(row);
                    rows.Clear();
                    diagram.RefreshDiagram();
                    Assert.Same(diagram.FindResource("Brush.pad-key"), FillOf(diagram.ButtonBHighlight));
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
    }
}
