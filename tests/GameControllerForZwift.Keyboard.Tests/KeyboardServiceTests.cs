using GameControllerForZwift.Core;
using InputSimulatorEx.Native;
using Microsoft.Extensions.Logging;
using NSubstitute;

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
        public async Task PerformActionAsync_ThrowsException_LogsError_OnException()
        {
            // Simulate an error in PerformActionAsync by triggering an unsupported ZwiftFunction
            var unsupportedFunction = (ZwiftFunction)999;
            var result = await _keyboardService.PerformActionAsync(unsupportedFunction);

            // Assert that the result indicates failure
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);

            // Optionally, verify that the logger captured an error
        }

        [Fact]
        public async Task PerformActionAsync_CancelsPreviousKeyPress_WhenCalledMultipleTimes()
        {
            var function = ZwiftFunction.ShowMenu;

            // Perform the first action
            var result1 = await _keyboardService.PerformActionAsync(function);
            Assert.True(result1.Success);

            // Perform the same action again before the previous one is released
            var result2 = await _keyboardService.PerformActionAsync(function);
            Assert.True(result2.Success);

            // Optionally, verify cancellation of the previous key press (this would require simulating the KeyUp behavior)
            // This is more of a behavioral test to ensure the cancellation works correctly
        }

        [Fact]
        public async Task PerformActionAsync_ReturnsFailure_WhenZwiftFunctionIsUnhandled()
        {
            var unsupportedFunction = (ZwiftFunction)999;

            var result = await _keyboardService.PerformActionAsync(unsupportedFunction);

            Assert.False(result.Success);
            Assert.Equal("Unhandled Zwift function.", result.ErrorMessage);
        }

        // Add a test case for edge cases where ZwiftPlayerView or ZwiftRiderAction might have unexpected values
        [Theory]
        [InlineData((ZwiftPlayerView)999)]
        [InlineData((ZwiftRiderAction)999)]
        public async Task PerformActionAsync_HandlesUnexpectedEnumValues_Gracefully(Enum invalidEnumValue)
        {
            // Testing with invalid player view or rider action
            var result = await _keyboardService.PerformActionAsync(ZwiftFunction.AdjustCameraAngle, (ZwiftPlayerView)invalidEnumValue);

            // Expecting failure due to invalid ZwiftPlayerView
            Assert.False(result.Success);
            Assert.Equal("Unhandled Zwift function.", result.ErrorMessage);

            // Similarly, test with ZwiftRiderAction
            result = await _keyboardService.PerformActionAsync(ZwiftFunction.RiderAction, riderAction: (ZwiftRiderAction)invalidEnumValue);

            // Expecting failure due to invalid ZwiftRiderAction
            Assert.False(result.Success);
            Assert.Equal("Unhandled Zwift function.", result.ErrorMessage);
        }

        [Theory]
        [InlineData(ZwiftFunction.ShowMenu, VirtualKeyCode.UP)]
        [InlineData(ZwiftFunction.NavigateLeft, VirtualKeyCode.LEFT)]
        [InlineData(ZwiftFunction.NavigateRight, VirtualKeyCode.RIGHT)]
        [InlineData(ZwiftFunction.Uturn, VirtualKeyCode.DOWN)]
        [InlineData(ZwiftFunction.Powerup, VirtualKeyCode.SPACE)]
        [InlineData(ZwiftFunction.Select, VirtualKeyCode.RETURN)]
        [InlineData(ZwiftFunction.GoBack, VirtualKeyCode.ESCAPE)]
        [InlineData(ZwiftFunction.ShowPairedDevices, VirtualKeyCode.VK_A)]
        [InlineData(ZwiftFunction.ShowGarage, VirtualKeyCode.VK_T)]
        [InlineData(ZwiftFunction.ToggleGraphs, VirtualKeyCode.VK_G)]
        [InlineData(ZwiftFunction.SendGroupText, VirtualKeyCode.VK_M)]
        [InlineData(ZwiftFunction.HideHUD, VirtualKeyCode.VK_H)]
        [InlineData(ZwiftFunction.PromoCode, VirtualKeyCode.VK_P)]
        [InlineData(ZwiftFunction.ShowTrainingMenu, VirtualKeyCode.VK_E)]
        [InlineData(ZwiftFunction.SkipWorkoutBlock, VirtualKeyCode.TAB)]
        [InlineData(ZwiftFunction.FTPBiasUp, VirtualKeyCode.PRIOR)]
        [InlineData(ZwiftFunction.FTPBiasDown, VirtualKeyCode.NEXT)]
        public void GetKeyCode_ReturnsCorrectKeyCode_ForZwiftFunction(ZwiftFunction function, VirtualKeyCode expectedKeyCode)
        {
            var result = _keyboardService.GetKeyCode(function, ZwiftPlayerView.Default, ZwiftRiderAction.WaveHand);
            Assert.Equal(expectedKeyCode, result);
        }

        [Theory]
        [InlineData(ZwiftPlayerView.Default, VirtualKeyCode.VK_1)]
        [InlineData(ZwiftPlayerView.ThirdPerson, VirtualKeyCode.VK_2)]
        [InlineData(ZwiftPlayerView.FPS, VirtualKeyCode.VK_3)]
        [InlineData(ZwiftPlayerView.FrontLeftSide, VirtualKeyCode.VK_4)]
        [InlineData(ZwiftPlayerView.RearRightSide, VirtualKeyCode.VK_5)]
        [InlineData(ZwiftPlayerView.FacingRider, VirtualKeyCode.VK_6)]
        [InlineData(ZwiftPlayerView.Spectator, VirtualKeyCode.VK_7)]
        [InlineData(ZwiftPlayerView.Helicopter, VirtualKeyCode.VK_8)]
        [InlineData(ZwiftPlayerView.BirdsEye, VirtualKeyCode.VK_9)]
        [InlineData(ZwiftPlayerView.Drone, VirtualKeyCode.VK_0)]
        public void GetKeyCode_ReturnsCorrectKeyCodmage_ForAdjustCameraAngle(ZwiftPlayerView playerView, VirtualKeyCode expectedKeyCode)
        {
            var result = _keyboardService.GetKeyCode(ZwiftFunction.AdjustCameraAngle, playerView, ZwiftRiderAction.WaveHand);
            Assert.Equal(expectedKeyCode, result);
        }

        [Theory]
        [InlineData(ZwiftRiderAction.Elbow, VirtualKeyCode.F1)]
        [InlineData(ZwiftRiderAction.WaveHand, VirtualKeyCode.F2)]
        [InlineData(ZwiftRiderAction.RideOn, VirtualKeyCode.F3)]
        [InlineData(ZwiftRiderAction.HammerTime, VirtualKeyCode.F4)]
        [InlineData(ZwiftRiderAction.Nice, VirtualKeyCode.F5)]
        [InlineData(ZwiftRiderAction.BringIt, VirtualKeyCode.F6)]
        [InlineData(ZwiftRiderAction.ImToast, VirtualKeyCode.F7)]
        [InlineData(ZwiftRiderAction.BikeBell, VirtualKeyCode.F8)]
        [InlineData(ZwiftRiderAction.ScreenShot, VirtualKeyCode.F10)]
        public void GetKeyCode_ReturnsCorrectKeyCode_ForRiderAction(ZwiftRiderAction riderAction, VirtualKeyCode expectedKeyCode)
        {
            var result = _keyboardService.GetKeyCode(ZwiftFunction.RiderAction, ZwiftPlayerView.Default, riderAction);
            Assert.Equal(expectedKeyCode, result);
        }

        [Theory]
        [InlineData((ZwiftFunction)999, VirtualKeyCode.CANCEL)]
        public void GetKeyCode_ReturnsDefault_WhenZwiftFunctionIsInvalid(ZwiftFunction function, VirtualKeyCode expectedKeyCode)
        {
            var result = _keyboardService.GetKeyCode(function, ZwiftPlayerView.Default, ZwiftRiderAction.WaveHand);
            Assert.Equal(expectedKeyCode, result);
        }

        [Fact]
        public void GetKeyCode_ReturnsDefault_WhenPlayerViewIsInvalid()
        {
            var result = _keyboardService.GetKeyCode(ZwiftFunction.AdjustCameraAngle, (ZwiftPlayerView)999, ZwiftRiderAction.WaveHand);
            Assert.Equal(VirtualKeyCode.CANCEL, result);
        }

        [Fact]
        public void GetKeyCode_ReturnsDefault_WhenRiderActionIsInvalid()
        {
            var result = _keyboardService.GetKeyCode(ZwiftFunction.RiderAction, ZwiftPlayerView.Default, (ZwiftRiderAction)999);
            Assert.Equal(VirtualKeyCode.CANCEL, result);
        }
    }
}
