using GameControllerForZwift.Core;
using InputSimulatorEx;
using InputSimulatorEx.Native;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GameControllerForZwift.Keyboard
{
    public class KeyboardServiceTests
    {
        private readonly IInputSimulator _simulatorMock;
        private readonly ILogger<KeyboardService> _loggerMock;
        private readonly KeyboardService _keyboardService;

        public KeyboardServiceTests()
        {
            _simulatorMock = Substitute.For<IInputSimulator>();
            _loggerMock = Substitute.For<ILogger<KeyboardService>>();
            _keyboardService = new KeyboardService(_loggerMock);
        }

        [Fact]
        public async Task PerformActionAsync_PerformsCorrectKeyPress_ForShowMenu()
        {
            // Arrange
            var zwiftFunction = ZwiftFunction.ShowMenu;

            // Act
            var result = await _keyboardService.PerformActionAsync(zwiftFunction);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task PerformActionAsync_ReturnsFalse_WhenActionIsWithinTimeout()
        {
            // Arrange
            var zwiftFunction = ZwiftFunction.NavigateLeft;

            await _keyboardService.PerformActionAsync(zwiftFunction); // Perform action first time
            var result = await _keyboardService.PerformActionAsync(zwiftFunction); // Perform action within timeout

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Action ignored due to recent execution.", result.ErrorMessage);
        }

        //[Fact]
        //public async Task PerformActionAsync_LogsError_WhenExceptionOccurs()
        //{
        //    // Arrange
        //    var zwiftFunction = ZwiftFunction.GoBack;

        //    // Simulate an exception in the key press
        //    _simulatorMock.Keyboard.When(k => k.KeyPress(VirtualKeyCode.ESCAPE)).Do(x => throw new Exception("Simulated exception"));

        //    // Act
        //    var result = await _keyboardService.PerformActionAsync(zwiftFunction);

        //    // Assert
        //    Assert.False(result.Success);
        //    Assert.Equal("Simulated exception", result.ErrorMessage);
        //    _loggerMock.Received(1).LogError(Arg.Any<Exception>(), Arg.Is<string>(s => s.Contains("Error writing to keyboard.")));
        //}

        [Fact]
        public async Task PerformActionAsync_ReturnsFalse_ForUnhandledZwiftFunction()
        {
            // Arrange
            var zwiftFunction = (ZwiftFunction)999; // Invalid function

            // Act
            var result = await _keyboardService.PerformActionAsync(zwiftFunction);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Unhandled Zwift Function selection.", result.ErrorMessage);
        }
    }
}
