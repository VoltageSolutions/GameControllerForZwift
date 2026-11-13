using GameControllerForZwift.Network.Dircon;

namespace GameControllerForZwift.Network.UnitTests;

public class KeypadFrameTests
{
    private static string Hex(byte[] b) => Convert.ToHexString(b);

    [Fact]
    public void Idle() => Assert.Equal("2308FFFFFFFF0F", Hex(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.None)));

    [Fact]
    public void Rs1() => Assert.Equal("2308FFDFFFFF0F", Hex(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.RS1)));

    [Fact]
    public void Ls1() => Assert.Equal("2308FFFDFFFF0F", Hex(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.LS1)));

    [Fact]
    public void Left() => Assert.Equal("2308FEFFFFFF0F", Hex(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.Left)));

    [Fact]
    public void Right_Up_Down_A()
    {
        Assert.Equal("2308FBFFFFFF0F", Hex(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.Right)));
        Assert.Equal("2308FDFFFFFF0F", Hex(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.Up)));
        Assert.Equal("2308F7FFFFFF0F", Hex(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.Down)));
        Assert.Equal("2308EFFFFFFF0F", Hex(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.A)));
    }

    [Fact]
    public void IdleRideData() =>
        Assert.Equal("03080010001800200028003000", Hex(ZwiftControlProtocol.BuildRideData(0, 0)));

    [Fact]
    public void RideData_WithPowerAndCadence() =>
        Assert.Equal("0308231048180020002800" + "3000", Hex(ZwiftControlProtocol.BuildRideData(35, 72)));

    [Fact]
    public void DeviceInfoReply_MatchesCapture_ForCaptureIdentity()
    {
        var id = new DeviceIdentity("KICKR BIKE SHIFT 0001", "100000001", "02-47-5A-00-00-01", "1.21.18", "3");
        var reply = new ZwiftControlProtocol(id).BuildDeviceInfoReply();
        Assert.Equal(
            "3c080012390a370882041204120015011a0c4b49434b522042494b452053320f3434313030303030303031000000003a0133420408011024420408021000",
            Hex(reply).ToLowerInvariant());
    }
}
