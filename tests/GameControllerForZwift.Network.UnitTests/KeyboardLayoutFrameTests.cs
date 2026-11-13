using GameControllerForZwift.Network.Dircon;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>Masks and frames used by the probe's keyboard layout (Zwift labels, BikeControl table confirmed by live tests and support.zwift.com).</summary>
public class KeyboardLayoutFrameTests
{
    private static string Hex(byte[] b) => Convert.ToHexString(b);

    private static string Frame(ZwiftRideButton buttons, int left, int right) =>
        Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(buttons, new RideAnalogEntry(0, left), new RideAnalogEntry(1, right)));

    [Fact]
    public void ButtonMasks_AreTheZwiftTable()
    {
        Assert.Equal(0x1u, (uint)ZwiftRideButton.Left);
        Assert.Equal(0x2u, (uint)ZwiftRideButton.Up);
        Assert.Equal(0x4u, (uint)ZwiftRideButton.Right);
        Assert.Equal(0x8u, (uint)ZwiftRideButton.Down);
        Assert.Equal(0x10u, (uint)ZwiftRideButton.A);
        Assert.Equal(0x20u, (uint)ZwiftRideButton.B);
        Assert.Equal(0x40u, (uint)ZwiftRideButton.Y);
        Assert.Equal(0x80u, (uint)ZwiftRideButton.Z);
        Assert.Equal(0x100u, (uint)ZwiftRideButton.LS1);
        Assert.Equal(0x200u, (uint)ZwiftRideButton.LS2);
        Assert.Equal(0x400u, (uint)ZwiftRideButton.LB);
        Assert.Equal(0x800u, (uint)ZwiftRideButton.LPower);
        Assert.Equal(0x1000u, (uint)ZwiftRideButton.RS1);
        Assert.Equal(0x2000u, (uint)ZwiftRideButton.RS2);
        Assert.Equal(0x4000u, (uint)ZwiftRideButton.RB);
        Assert.Equal(0x8000u, (uint)ZwiftRideButton.RPower);
    }

    [Fact]
    public void ShiftTaps_PressAndRelease()
    {
        Assert.Equal("2308FFDFFFFF0F1A04080010001A0408011000", Frame(ZwiftRideButton.RS1, 0, 0));   // '=' RS1 (harder)
        Assert.Equal("2308FFBFFFFF0F1A04080010001A0408011000", Frame(ZwiftRideButton.RS2, 0, 0));   // '0' RS2 (harder)
        Assert.Equal("2308FFFDFFFF0F1A04080010001A0408011000", Frame(ZwiftRideButton.LS1, 0, 0));   // '-' LS1 (easier)
        Assert.Equal("2308FFFBFFFF0F1A04080010001A0408011000", Frame(ZwiftRideButton.LS2, 0, 0));   // '9' LS2 (easier)
        Assert.Equal("2308FFFEFFFF0F1A04080010001A0408011000", Frame(ZwiftRideButton.Z, 0, 0));     // 'z' Z
        Assert.Equal("2308FFFFFFFF0F1A04080010001A0408011000", Frame(ZwiftRideButton.None, 0, 0));          // release = neutral
    }

    [Fact]
    public void Steer_PaddlesOutward()
    {
        Assert.Equal("2308FFFFFFFF0F1A05080010C7011A0408011000", Frame(ZwiftRideButton.None, -100, 0)); // q full left
        Assert.Equal("2308FFFFFFFF0F1A04080010631A0408011000", Frame(ZwiftRideButton.None, -50, 0));    // w partial left
        Assert.Equal("2308FFFFFFFF0F1A04080010001A0408011064", Frame(ZwiftRideButton.None, 0, 50));     // e partial right
        Assert.Equal("2308FFFFFFFF0F1A04080010001A05080110C801", Frame(ZwiftRideButton.None, 0, 100));  // r full right
    }

    [Fact]
    public void Brake_PaddlesInward()
    {
        Assert.Equal("2308FFFFFFFF0F1A05080010C8011A0408011000", Frame(ZwiftRideButton.None, 100, 0));  // a left full
        Assert.Equal("2308FFFFFFFF0F1A04080010641A0408011000", Frame(ZwiftRideButton.None, 50, 0));     // s left partial
        Assert.Equal("2308FFFFFFFF0F1A04080010001A0408011063", Frame(ZwiftRideButton.None, 0, -50));    // d right partial
        Assert.Equal("2308FFFFFFFF0F1A04080010001A05080110C701", Frame(ZwiftRideButton.None, 0, -100)); // f right full
    }

    [Fact]
    public void ShiftTap_MergesIntoAHeldPaddleFrame()
    {
        Assert.Equal("2308FFDFFFFF0F1A05080010C7011A0408011000", Frame(ZwiftRideButton.RS1, -100, 0));
    }
}
