using GameControllerForZwift.Network.DirectConnect;

namespace GameControllerForZwift.Network.UnitTests;

public class DirectConnectMessageTests
{
    [Fact]
    public void Encode_ThenParse_RoundTrips()
    {
        var msg = DirectConnectMessage.Create(DirectConnectMessageId.ReadCharacteristic, 0x2A, DirectConnectResponseCode.Success, new byte[] { 1, 2, 3 });
        var bytes = msg.Encode();

        Assert.Equal(new byte[] { 1, 3, 0x2A, 0, 0, 3, 1, 2, 3 }, bytes);
        Assert.True(DirectConnectMessage.TryParse(bytes, out var parsed, out int consumed));
        Assert.Equal(bytes.Length, consumed);
        Assert.Equal(1, parsed!.Version);
        Assert.Equal(DirectConnectMessageId.ReadCharacteristic, parsed.MessageId);
        Assert.Equal(0x2A, parsed.Seq);
        Assert.Equal(0, parsed.ResponseCode);
        Assert.Equal(new byte[] { 1, 2, 3 }, parsed.Payload);
    }

    [Fact]
    public void Length_IsBigEndian()
    {
        var msg = DirectConnectMessage.Create(DirectConnectMessageId.Notification, 0, 0, new byte[300]);
        var bytes = msg.Encode();
        Assert.Equal(0x01, bytes[4]);
        Assert.Equal(0x2C, bytes[5]);
    }

    [Fact]
    public void Parse_TwoConcatenatedMessages_ConsumesOneAtATime()
    {
        var a = DirectConnectMessage.Create(1, 0, 0).Encode();
        var b = DirectConnectMessage.Create(2, 1, 0, new byte[16]).Encode();
        var both = a.Concat(b).ToArray();

        Assert.True(DirectConnectMessage.TryParse(both, out var m1, out int c1));
        Assert.Equal(a.Length, c1);
        Assert.Equal(1, m1!.MessageId);

        Assert.True(DirectConnectMessage.TryParse(both.AsSpan(c1), out var m2, out int c2));
        Assert.Equal(b.Length, c2);
        Assert.Equal(2, m2!.MessageId);
        Assert.Equal(16, m2.Payload.Length);
    }

    [Fact]
    public void Parse_PartialMessage_ReturnsFalse()
    {
        var full = DirectConnectMessage.Create(4, 9, 0, new byte[20]).Encode();

        Assert.False(DirectConnectMessage.TryParse(full.AsSpan(0, 3), out var m, out int consumed));
        Assert.Null(m);
        Assert.Equal(0, consumed);

        Assert.False(DirectConnectMessage.TryParse(full.AsSpan(0, full.Length - 1), out _, out consumed));
        Assert.Equal(0, consumed);

        Assert.True(DirectConnectMessage.TryParse(full, out _, out _));
    }
}
