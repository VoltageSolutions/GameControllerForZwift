using GameControllerForZwift.Network.ZwiftRide;

namespace GameControllerForZwift.Network.UnitTests;

public class AnalogInputTests
{
    private static string Hex(byte[] b) => Convert.ToHexString(b);

    [Fact]
    public void BatteryReply_Default100_MatchesCapture() =>
        Assert.Equal("3C08830612080800106418002000", Hex(ZwiftControlProtocol.BuildBatteryReply(100)));

    [Theory]
    [InlineData(55, "3C08830612080800103718002000")]
    [InlineData(0, "3C08830612080800100018002000")]
    [InlineData(250, "3C08830612080800106418002000")] // clamped
    public void BatteryReply_Parameterized(int pct, string expected) =>
        Assert.Equal(expected, Hex(ZwiftControlProtocol.BuildBatteryReply(pct)));

    [Fact]
    public void RideKeyPad_NeutralWithBothEntries_MatchesCapture() =>
        Assert.Equal("2308FFFFFFFF0F1A0408001000" + "1A0408011000",
            Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, 0), new RideAnalogEntry(1, 0))));

    [Fact]
    public void RideKeyPad_Paddles_ZigZagEncoded()
    {
        // -100 -> zigzag 199 = c7 01; +100 -> 200 = c8 01
        var frame = ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.RS1, new RideAnalogEntry(0, -100), new RideAnalogEntry(1, 100));
        Assert.EndsWith("1A05080010C701" + "1A05080110C801", Hex(frame));
    }

    [Fact]
    public void RideKeyPad_AnalogEntries_MatchCaptureLayout()
    {
        // capture: loc 0 = -32768 (65535 zigzag), loc 1 = 0
        Assert.Equal("2308FFFFFFFF0F1A06080010FFFF031A0408011000",
            Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, -32768), new RideAnalogEntry(1, 0))));
        // capture: loc 0 = 0, loc 1 = +32767 (65534 zigzag)
        Assert.Equal("2308FFFFFFFF0F1A04080010001A06080110FEFF03",
            Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, 0), new RideAnalogEntry(1, 32767))));
        // Zwift Ride style full paddle: left -100 (zigzag 199 = c7 01)
        Assert.Equal("2308FFFFFFFF0F1A050800" + "10C701",
            Hex(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, -100))));
    }

    [Theory]
    [InlineData("2308FFFFFFFF0F", "pressed=0x00000000 analog=[]")]
    [InlineData("2308FFDFFFFF0F1A05080010C7011A05080110C801", "pressed=0x00001000 analog=[loc0=-100, loc1=100]")]
    public void DescribeKeyPadFrame_DecodesFrames(string hex, string expected)
        => Assert.Equal(expected, ZwiftControlProtocol.DescribeKeyPadFrame(Convert.FromHexString(hex)));

    [Fact]
    public void DescribeKeyPadFrame_RejectsOtherFrames()
        => Assert.Equal("not a 0x23 frame", ZwiftControlProtocol.DescribeKeyPadFrame(new byte[] { 0x03, 0x08, 0x00 }));
}
