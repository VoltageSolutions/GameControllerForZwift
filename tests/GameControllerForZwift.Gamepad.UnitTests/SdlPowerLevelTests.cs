using GameControllerForZwift.Gamepad.SDL2;
using static SDL2.SDL;

namespace GameControllerForZwift.Gamepad.UnitTests;

public class SdlPowerLevelTests
{
    [Theory]
    [InlineData(SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_EMPTY, 5)]
    [InlineData(SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_LOW, 20)]
    [InlineData(SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_MEDIUM, 70)]
    [InlineData(SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_FULL, 100)]
    [InlineData(SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_WIRED, 100)]
    public void MapPowerLevel_MapsKnownLevelsToPercent(SDL_JoystickPowerLevel level, int expected)
        => Assert.Equal(expected, SDL2GameController.MapPowerLevel(level));

    [Theory]
    [InlineData(SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_UNKNOWN)]
    [InlineData(SDL_JoystickPowerLevel.SDL_JOYSTICK_POWER_MAX)]
    [InlineData((SDL_JoystickPowerLevel)99)]
    public void MapPowerLevel_UnknownLevels_ReturnNull(SDL_JoystickPowerLevel level)
        => Assert.Null(SDL2GameController.MapPowerLevel(level));
}
