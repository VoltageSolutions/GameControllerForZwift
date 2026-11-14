using GameControllerForZwift.Network.ZwiftRide;

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
}
