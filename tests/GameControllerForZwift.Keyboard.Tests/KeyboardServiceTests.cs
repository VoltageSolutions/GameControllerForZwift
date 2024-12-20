using GameControllerForZwift.Core;
using InputSimulatorEx;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace GameControllerForZwift.Keyboard
{
    public class KeyboardServiceTests
    {
        private readonly ILogger<KeyboardService> _loggerMock;
        private readonly KeyboardService _keyboardService;

        public KeyboardServiceTests()
        {
            _loggerMock = Substitute.For<ILogger<KeyboardService>>();
            _keyboardService = new KeyboardService(_loggerMock);
        }

        [Theory]
        [InlineData(ZwiftFunction.ShowMenu)]
        [InlineData(ZwiftFunction.NavigateLeft)]
        [InlineData(ZwiftFunction.NavigateRight)]
        [InlineData(ZwiftFunction.Uturn)]
        [InlineData(ZwiftFunction.Powerup)]
        [InlineData(ZwiftFunction.Select)]
        [InlineData(ZwiftFunction.GoBack)]
        [InlineData(ZwiftFunction.ShowPairedDevices)]
        [InlineData(ZwiftFunction.ShowGarage)]
        [InlineData(ZwiftFunction.ToggleGraphs)]
        [InlineData(ZwiftFunction.SendGroupText)]
        [InlineData(ZwiftFunction.HideHUD)]
        [InlineData(ZwiftFunction.PromoCode)]
        [InlineData(ZwiftFunction.ShowTrainingMenu)]
        [InlineData(ZwiftFunction.SkipWorkoutBlock)]
        [InlineData(ZwiftFunction.FTPBiasUp)]
        [InlineData(ZwiftFunction.FTPBiasDown)]
        public async Task PerformActionAsync_PerformsCorrectKeyPress_ForZwiftFunction(ZwiftFunction function)
        {
            var result = await _keyboardService.PerformActionAsync(function);
            Assert.True(result.Success);
        }

        [Theory]
        [InlineData(ZwiftFunction.AdjustCameraAngle, ZwiftPlayerView.Default)]
        [InlineData(ZwiftFunction.AdjustCameraAngle, ZwiftPlayerView.BirdsEye)]
        [InlineData(ZwiftFunction.AdjustCameraAngle, ZwiftPlayerView.ThirdPerson)]
        [InlineData(ZwiftFunction.AdjustCameraAngle, ZwiftPlayerView.FrontLeftSide)]
        public async Task PerformActionAsync_PerformsCorrectKeyPress_ForZwiftPlayerView(ZwiftFunction function, ZwiftPlayerView view)
        {
            var result = await _keyboardService.PerformActionAsync(function, view);
            Assert.True(result.Success);
        }

        [Theory]
        [InlineData(ZwiftFunction.RiderAction, ZwiftRiderAction.WaveHand)]
        [InlineData(ZwiftFunction.RiderAction, ZwiftRiderAction.ImToast)]
        [InlineData(ZwiftFunction.RiderAction, ZwiftRiderAction.HammerTime)]
        [InlineData(ZwiftFunction.RiderAction, ZwiftRiderAction.RideOn)]
        public async Task PerformActionAsync_PerformsCorrectKeyPress_ForZwiftRiderAction(ZwiftFunction function, ZwiftRiderAction riderAction)
        {
            var result = await _keyboardService.PerformActionAsync(function, riderAction: riderAction);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task PerformActionAsync_ReturnsFalse_WhenActionIsWithinTimeout()
        {
            var zwiftFunction = ZwiftFunction.NavigateLeft;

            await _keyboardService.PerformActionAsync(zwiftFunction);
            var result = await _keyboardService.PerformActionAsync(zwiftFunction);

            Assert.False(result.Success);
            Assert.Equal("Action ignored due to recent execution.", result.ErrorMessage);
        }

        [Theory]
        [InlineData((ZwiftFunction)999, null, null, "Unhandled Zwift Function selection.")]
        [InlineData(ZwiftFunction.AdjustCameraAngle, (ZwiftPlayerView)999, null, "Unhandled ZwiftPlayerView selection.")]
        [InlineData(ZwiftFunction.RiderAction, null, (ZwiftRiderAction)999, "Unhandled ZwiftRiderAction selection.")]
        public async Task PerformActionAsync_ReturnsFalse_ForUnhandledInputs(ZwiftFunction function, ZwiftPlayerView? playerView, ZwiftRiderAction? riderAction, string expectedErrorMessage)
        {
            var result = await _keyboardService.PerformActionAsync(function, playerView ?? ZwiftPlayerView.Default, riderAction ?? ZwiftRiderAction.RideOn);

            Assert.False(result.Success);
            Assert.Equal(expectedErrorMessage, result.ErrorMessage);
        }
    }
}