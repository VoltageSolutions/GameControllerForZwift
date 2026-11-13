using GameControllerForZwift.Network.Dircon;

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
}
