using GameControllerForZwift.Gamepad.SDL2;

namespace GameControllerForZwift.Gamepad.UnitTests;

public class SdlDeviceDiagnosticsTests
{
    private static SdlDeviceInfo Device(bool isGameController, string? name = "Pad", byte[]? guid = null)
        => new(2, name, guid ?? [0x03, 0x00, 0xAB, 0xFF], 0x2DC8, 0x6101, "SDL_JOYSTICK_TYPE_GAMECONTROLLER", isGameController);

    [Fact]
    public void ShouldInclude_GameController_IsIncluded()
    {
        Assert.True(SdlDeviceDiagnostics.ShouldInclude(Device(true), out string? reason));
        Assert.Null(reason);
    }

    [Fact]
    public void ShouldInclude_PlainJoystick_IsSkippedWithReason()
    {
        Assert.False(SdlDeviceDiagnostics.ShouldInclude(Device(false), out string? reason));
        Assert.False(string.IsNullOrWhiteSpace(reason));
    }

    [Fact]
    public void FormatGuid_RendersLowercaseHex()
        => Assert.Equal("0300abff", SdlDeviceDiagnostics.FormatGuid([0x03, 0x00, 0xAB, 0xFF]));

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    public void FormatGuid_EmptyOrNull_ReturnsPlaceholder(byte[]? bytes)
        => Assert.Equal("(none)", SdlDeviceDiagnostics.FormatGuid(bytes));

    [Fact]
    public void Format_IncludedDevice_ListsAllFields()
    {
        string line = SdlDeviceDiagnostics.Format(Device(true), null);

        Assert.Equal(
            "Device 2: name='Pad', guid=0300abff, vid=2DC8, pid=6101, type=SDL_JOYSTICK_TYPE_GAMECONTROLLER, isGameController=True, included",
            line);
    }

    [Fact]
    public void Format_SkippedDevice_IncludesReasonAndHandlesMissingName()
    {
        string line = SdlDeviceDiagnostics.Format(Device(false, name: null), "no mapping");

        Assert.Contains("name='(none)'", line, StringComparison.Ordinal);
        Assert.Contains("isGameController=False", line, StringComparison.Ordinal);
        Assert.EndsWith("skipped (no mapping)", line, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatRemoval_WithSdlEvent_ShowsBothDelays()
    {
        string line = SdlDeviceDiagnostics.FormatRemoval(7, new DateTime(2026, 11, 17, 17, 6, 39, 199), 312, 500);

        Assert.Equal(
            "Device removal detected at 17:06:39.199: instance 7. SDL reported it 312 ms earlier; the previous check ran 500 ms earlier.",
            line);
    }

    [Fact]
    public void FormatRemoval_WithoutSdlEventOrPreviousCheck_SaysSo()
    {
        string line = SdlDeviceDiagnostics.FormatRemoval(3, new DateTime(2026, 11, 17, 8, 0, 0, 5), null, null);

        Assert.Equal(
            "Device removal detected at 08:00:00.005: instance 3. SDL queued no removal event for it; this is the first check.",
            line);
    }
}
