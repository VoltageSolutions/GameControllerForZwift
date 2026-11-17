using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Dialogs;

namespace GameControllerForZwift.UI.WPF.Tests.Dialogs
{
    /// <summary>
    /// CFG-038: dialogs can be used with a game controller.
    /// </summary>
    public class DialogNavigationTranslatorTests
    {
        [Fact]
        public void CFG038_FirstReadingIsBaseline_HeldButtonDoesNothing()
        {
            var translator = new DialogNavigationTranslator();

            Assert.Equal(DialogNavigationAction.None, translator.Translate(new ControllerData { A = true }));
            Assert.Equal(DialogNavigationAction.None, translator.Translate(new ControllerData { A = true }));
        }

        [Theory]
        [InlineData(nameof(ControllerData.A), DialogNavigationAction.Activate)]
        [InlineData(nameof(ControllerData.B), DialogNavigationAction.Cancel)]
        [InlineData(nameof(ControllerData.DPadDown), DialogNavigationAction.Next)]
        [InlineData(nameof(ControllerData.DPadRight), DialogNavigationAction.Next)]
        [InlineData(nameof(ControllerData.DPadUp), DialogNavigationAction.Previous)]
        [InlineData(nameof(ControllerData.DPadLeft), DialogNavigationAction.Previous)]
        public void CFG038_NewPressMapsToAction(string button, DialogNavigationAction expected)
        {
            var translator = new DialogNavigationTranslator();
            translator.Translate(new ControllerData());
            var pressed = new ControllerData();
            typeof(ControllerData).GetProperty(button)!.SetValue(pressed, true);

            Assert.Equal(expected, translator.Translate(pressed));
        }

        [Fact]
        public void CFG038_HoldingButton_ActsOnceUntilReleased()
        {
            var translator = new DialogNavigationTranslator();
            translator.Translate(new ControllerData());

            Assert.Equal(DialogNavigationAction.Next, translator.Translate(new ControllerData { DPadDown = true }));
            Assert.Equal(DialogNavigationAction.None, translator.Translate(new ControllerData { DPadDown = true }));
            Assert.Equal(DialogNavigationAction.None, translator.Translate(new ControllerData()));
            Assert.Equal(DialogNavigationAction.Next, translator.Translate(new ControllerData { DPadDown = true }));
        }
    }
}
