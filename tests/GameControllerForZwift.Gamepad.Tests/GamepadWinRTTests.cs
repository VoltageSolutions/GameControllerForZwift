//using GameControllerForZwift.Core;
//using NSubstitute;
//using Windows.Gaming.Input;

//namespace GameControllerForZwift.GamepadWinRT.Tests
//{
    //public class GamepadWinRTTests
    //{
    //    [Fact]
    //    public void AsControllers_ShouldReturnEmpty_WhenNoGamepadsConnected()
    //    {
    //        // Arrange
    //        var emptyGamepadsList = new List<Gamepad>();

    //        // Act
    //        var controllers = emptyGamepadsList.AsControllers();

    //        // Assert
    //        Assert.Empty(controllers);
    //    }

    //    // Cannot test having controllers without real hardware - there is no available constructor for Gamepad

    //    //[Fact]
    //    //public void AsControllers_ShouldReturnControllers_WhenGamepadsAreConnected()
    //    //{
    //    //    // Arrange
    //    //    var mockGamepad1 = Substitute.For<Gamepad>();
    //    //    var mockGamepad2 = Substitute.For<Gamepad>();
    //    //    var gamepadsList = new List<Gamepad> { mockGamepad1, mockGamepad2 };

    //    //    // Act
    //    //    var controllers = gamepadsList.AsControllers();

    //    //    // Assert
    //    //    Assert.Equal(2, controllers.Count());
    //    //}

    //    [Fact]
    //    public void AsControllerData_ConvertsGamepadReadingCorrectly()
    //    {
    //        // Arrange
    //        var reading = new GamepadReading
    //        {
    //            Buttons = GamepadButtons.A | GamepadButtons.B,
    //            LeftThumbstickX = 0.1,
    //            LeftThumbstickY = 0.2,
    //            LeftTrigger = 0.3,
    //            RightThumbstickX = 0.4,
    //            RightThumbstickY = 0.5,
    //            RightTrigger = 0.6,
    //            Timestamp = 1633025800
    //        };

    //        // Act
    //        var data = reading.AsControllerData();

    //        // Assert
    //        Assert.Equal(ControllerButtons.A | ControllerButtons.B, data.Buttons);
    //        Assert.Equal(0.1, data.LeftThumbstickX);
    //        Assert.Equal(0.2, data.LeftThumbstickY);
    //        Assert.Equal(0.3, data.LeftTrigger);
    //        Assert.Equal(0.4, data.RightThumbstickX);
    //        Assert.Equal(0.5, data.RightThumbstickY);
    //        Assert.Equal(0.6, data.RightTrigger);
    //        Assert.Equal(new DateTime(1970, 1, 1).AddSeconds(1633025800), data.Timestamp);
    //    }

    //    [Fact]
    //    public void ConvertFromUnixTimestamp_ZeroTimestamp_ReturnsEpoch()
    //    {
    //        // Arrange
    //        ulong timestamp = 0;

    //        // Act
    //        DateTime result = GamepadWinRT.ConvertFromUnixTimestamp(timestamp);

    //        // Assert
    //        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    //    }

    //    [Fact]
    //    public void ConvertFromUnixTimestamp_PositiveTimestamp_ReturnsCorrectDateTime()
    //    {
    //        // Arrange
    //        ulong timestamp = 1633025800; // Represents Sat, 30 Sep 2021 6:16:40 UTC

    //        // Act
    //        DateTime result = GamepadWinRT.ConvertFromUnixTimestamp(timestamp);

    //        // Assert
    //        Assert.Equal(new DateTime(2021, 9, 30, 18, 16, 40, DateTimeKind.Utc), result);
    //    }

    //    [Fact]
    //    public void ConvertFromUnixTimestamp_LargeTimestamp_ReturnsCorrectDateTime()
    //    {
    //        // Arrange
    //        ulong timestamp = 253402300799; // Represents Sun, 31 Dec 9999 23:59:59 UTC

    //        // Act
    //        DateTime result = GamepadWinRT.ConvertFromUnixTimestamp(timestamp);

    //        // Assert
    //        Assert.Equal(new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc), result);
    //    }
    //}
//}
