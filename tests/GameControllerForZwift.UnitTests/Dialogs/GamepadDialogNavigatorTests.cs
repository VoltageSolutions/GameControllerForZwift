using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Dialogs;
using NSubstitute;
using System.Windows;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.Tests.Dialogs
{
    /// <summary>
    /// CFG-038: a game controller drives an open dialog.
    /// </summary>
    [Collection(GameControllerForZwift.UnitTests.WpfWindows.Name)]
    public class GamepadDialogNavigatorTests
    {
        private static void Press(IDataIntegrator integrator, ControllerData data) =>
            integrator.InputPolled += Raise.EventWith(integrator, new InputStateChangedEventArgs(data));

        /// <summary>
        /// Shows the dialog, sends a baseline reading and then the given press, and closes the dialog if the press did not.
        /// </summary>
        private static bool PressAndShow(Window dialog, ControllerData press, bool closeAfterwards)
        {
            var integrator = Substitute.For<IDataIntegrator>();
            dialog.ShowInTaskbar = false;
            using var navigator = new GamepadDialogNavigator(integrator, dialog);
            var closedByPress = true;

            dialog.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                Press(integrator, new ControllerData());
                Press(integrator, press);
                if (closeAfterwards)
                {
                    dialog.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
                    {
                        closedByPress = false;
                        dialog.Close();
                    });
                }
            });
            dialog.ShowDialog();
            return closedByPress;
        }

        [Fact]
        public void CFG038_BPress_ClosesDialog()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new MessageDialogWindow("T", "M", "Yes", null, "No");

                Assert.True(PressAndShow(dialog, new ControllerData { B = true }, closeAfterwards: true));
            });
        }

        [Fact]
        public void CFG038_APress_ActivatesDefaultButton()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new MessageDialogWindow("T", "M", null, null, "OK");

                Assert.True(PressAndShow(dialog, new ControllerData { A = true }, closeAfterwards: true));
            });
        }

        [Theory]
        [InlineData(nameof(ControllerData.DPadDown))]
        [InlineData(nameof(ControllerData.DPadUp))]
        public void CFG038_DpadPress_MovesFocusWithoutClosing(string button)
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var dialog = new MessageDialogWindow("T", "M", "Yes", "Maybe", "No");
                var data = new ControllerData();
                typeof(ControllerData).GetProperty(button)!.SetValue(data, true);

                Assert.False(PressAndShow(dialog, data, closeAfterwards: true));
            });
        }

        [Fact]
        public void CFG038_DisposedNavigator_IgnoresFurtherInput()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                var integrator = Substitute.For<IDataIntegrator>();
                var dialog = new MessageDialogWindow("T", "M", null, null, "OK");
                var navigator = new GamepadDialogNavigator(integrator, dialog);
                navigator.Dispose();

                Press(integrator, new ControllerData());
                Press(integrator, new ControllerData { B = true });

                Assert.False(dialog.IsVisible);
            });
        }
    }
}
