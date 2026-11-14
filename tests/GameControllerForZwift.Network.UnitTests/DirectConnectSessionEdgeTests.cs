using GameControllerForZwift.Network.DirectConnect;
using GameControllerForZwift.Network.ZwiftRide;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>Error codes, queueing and framing behaviour of <see cref="DirectConnectSession"/>.</summary>
public class DirectConnectSessionEdgeTests
{
    private static byte[] Cat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
    private static byte[] Req(byte id, byte seq, byte[] payload, byte rc = 0) => DirectConnectMessage.Create(id, seq, rc, payload).Encode();

    private static DirectConnectSession NewSession(LoopbackPair pair)
        => new(pair.Server, ZwiftRideProfile.CreateServices(), new ZwiftControlProtocol(), NullLogger.Instance, "test");

    private static async Task<DirectConnectMessage> RoundTripAsync(LoopbackPair pair, byte[] request)
    {
        await pair.Client.WriteAsync(request);
        return (await pair.ReadMessagesAsync(1))[0];
    }

    [Fact]
    public async Task UnknownMessageId_RepliesUnexpectedError()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var reply = await RoundTripAsync(pair, Req(0x7F, 9, Array.Empty<byte>()));
        Assert.Equal(DirectConnectResponseCode.UnexpectedError, reply.ResponseCode);
        Assert.Equal(9, reply.Seq);
        Assert.Equal(0x7F, reply.MessageId);
    }

    [Fact]
    public async Task RequestsWithNonZeroResponseCode_AreIgnored()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        await pair.Client.WriteAsync(Req(DirectConnectMessageId.DiscoverServices, 1, Array.Empty<byte>(), rc: 3));
        var reply = await RoundTripAsync(pair, Req(DirectConnectMessageId.DiscoverServices, 2, Array.Empty<byte>()));
        Assert.Equal(2, reply.Seq); // the first request got no answer
    }

    [Fact]
    public async Task DiscoverCharacteristics_BadLengthAndUnknownService()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var bad = await RoundTripAsync(pair, Req(DirectConnectMessageId.DiscoverCharacteristics, 1, new byte[4]));
        Assert.Equal(DirectConnectResponseCode.UnexpectedError, bad.ResponseCode);
        var unknown = await RoundTripAsync(pair, Req(DirectConnectMessageId.DiscoverCharacteristics, 2, BleUuid.FromShort(0x1234).ToBytes()));
        Assert.Equal(DirectConnectResponseCode.ServiceNotFound, unknown.ResponseCode);
    }

    [Theory]
    [InlineData(DirectConnectMessageId.ReadCharacteristic)]
    [InlineData(DirectConnectMessageId.WriteCharacteristic)]
    [InlineData(DirectConnectMessageId.EnableNotifications)]
    public async Task ShortPayload_RepliesUnexpectedError_UnknownCharacteristic_RepliesNotFound(byte id)
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var shortReply = await RoundTripAsync(pair, Req(id, 1, new byte[3]));
        Assert.Equal(DirectConnectResponseCode.UnexpectedError, shortReply.ResponseCode);
        var notFound = await RoundTripAsync(pair, Req(id, 2, Cat(BleUuid.FromShort(0x1234).ToBytes(), new byte[] { 1 })));
        Assert.Equal(DirectConnectResponseCode.CharacteristicNotFound, notFound.ResponseCode);
    }

    [Fact]
    public async Task OperationsNotAllowedByProperties_ReplyOperationNotSupported()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        // 2A29 manufacturer name: read-only. 2AD2 indoor bike data: notify only.
        var write = await RoundTripAsync(pair, Req(DirectConnectMessageId.WriteCharacteristic, 1, Cat(ZwiftRideUuids.ManufacturerName.ToBytes(), new byte[] { 1 })));
        Assert.Equal(DirectConnectResponseCode.OperationNotSupported, write.ResponseCode);
        var notify = await RoundTripAsync(pair, Req(DirectConnectMessageId.EnableNotifications, 2, Cat(ZwiftRideUuids.ManufacturerName.ToBytes(), new byte[] { 1 })));
        Assert.Equal(DirectConnectResponseCode.OperationNotSupported, notify.ResponseCode);
        var read = await RoundTripAsync(pair, Req(DirectConnectMessageId.ReadCharacteristic, 3, ZwiftRideUuids.IndoorBikeData.ToBytes()));
        Assert.Equal(DirectConnectResponseCode.OperationNotSupported, read.ResponseCode);
    }

    [Fact]
    public async Task Write_AckEchoesSeqAndCharacteristicUuid()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var uuid = ZwiftRideUuids.SyncRx;
        var ack = await RoundTripAsync(pair, Req(DirectConnectMessageId.WriteCharacteristic, 77, Cat(uuid.ToBytes(), new byte[] { 0x99, 0x01 })));
        Assert.Equal(77, ack.Seq);
        Assert.Equal(DirectConnectResponseCode.Success, ack.ResponseCode);
        Assert.Equal(uuid.ToBytes(), ack.Payload);
    }

    [Fact]
    public async Task SubscribeBeforeNotify_QueuesAndFlushesAfterAck()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var uuid = ZwiftRideUuids.IndoorBikeData;

        Assert.False(await session.NotifyAsync(uuid, new byte[] { 1 }, queueIfNotSubscribed: true));
        Assert.False(await session.NotifyAsync(uuid, new byte[] { 2 }, queueIfNotSubscribed: true));
        Assert.False(await session.NotifyAsync(uuid, new byte[] { 3 }, queueIfNotSubscribed: false)); // dropped
        Assert.False(session.IsSubscribed(uuid));

        await pair.Client.WriteAsync(Req(DirectConnectMessageId.EnableNotifications, 5, Cat(uuid.ToBytes(), new byte[] { 1 })));
        var msgs = await pair.ReadMessagesAsync(3);

        Assert.Equal(DirectConnectMessageId.EnableNotifications, msgs[0].MessageId);
        Assert.Equal(5, msgs[0].Seq);
        Assert.Equal(uuid.ToBytes(), msgs[0].Payload);
        Assert.Equal(DirectConnectMessageId.Notification, msgs[1].MessageId);
        Assert.Equal(Cat(uuid.ToBytes(), new byte[] { 1 }), msgs[1].Payload);
        Assert.Equal(Cat(uuid.ToBytes(), new byte[] { 2 }), msgs[2].Payload);
        Assert.True(session.IsSubscribed(uuid));

        // now subscribed: immediate send
        Assert.True(await session.NotifyAsync(uuid, new byte[] { 9 }, false));
        var live = (await pair.ReadMessagesAsync(1))[0];
        Assert.Equal(Cat(uuid.ToBytes(), new byte[] { 9 }), live.Payload);
        Assert.Equal(0, live.Seq);
    }

    [Fact]
    public async Task PendingQueue_IsCappedAtNewest64()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var uuid = ZwiftRideUuids.IndoorBikeData;
        for (int i = 0; i < 70; i++)
            await session.NotifyAsync(uuid, new[] { (byte)i }, true);

        await pair.Client.WriteAsync(Req(DirectConnectMessageId.EnableNotifications, 1, Cat(uuid.ToBytes(), new byte[] { 1 })));
        var msgs = await pair.ReadMessagesAsync(65);
        Assert.Equal(65, msgs.Count);
        Assert.Equal(6, msgs[1].Payload[16]);   // oldest 6 dropped
        Assert.Equal(69, msgs[^1].Payload[16]);
    }

    [Fact]
    public async Task DisableNotifications_Unsubscribes()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var uuid = ZwiftRideUuids.IndoorBikeData;
        await RoundTripAsync(pair, Req(DirectConnectMessageId.EnableNotifications, 1, Cat(uuid.ToBytes(), new byte[] { 1 })));
        Assert.True(session.IsSubscribed(uuid));
        await RoundTripAsync(pair, Req(DirectConnectMessageId.EnableNotifications, 2, Cat(uuid.ToBytes(), new byte[] { 0 })));
        Assert.False(session.IsSubscribed(uuid));
    }

    [Fact]
    public async Task EnableWithoutFlagByte_DefaultsToEnable()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        await RoundTripAsync(pair, Req(DirectConnectMessageId.EnableNotifications, 1, ZwiftRideUuids.IndoorBikeData.ToBytes()));
        Assert.True(session.IsSubscribed(ZwiftRideUuids.IndoorBikeData));
    }

    [Fact]
    public async Task FragmentedAndPipelinedFrames_AreReassembled()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var a = Req(DirectConnectMessageId.DiscoverServices, 1, Array.Empty<byte>());
        var b = Req(DirectConnectMessageId.DiscoverServices, 2, Array.Empty<byte>());
        var all = Cat(a, b);
        foreach (var chunk in all.Chunk(4))
        {
            await pair.Client.WriteAsync(chunk);
            await pair.Client.FlushAsync();
        }
        var msgs = await pair.ReadMessagesAsync(2);
        Assert.Equal(new byte[] { 1, 2 }, msgs.Select(m => m.Seq).ToArray());
    }

    [Fact]
    public async Task ClientDisconnect_EndsRun()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        await pair.Client.DisposeAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Dispose_IsIdempotent_AndStopsRun()
    {
        using var pair = await LoopbackPair.CreateAsync();
        var session = NewSession(pair);
        var run = session.RunAsync();
        await session.DisposeAsync();
        await session.DisposeAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Constructor_RejectsNulls()
    {
        var protocol = new ZwiftControlProtocol();
        var services = ZwiftRideProfile.CreateServices();
        Assert.Throws<ArgumentNullException>(() => new DirectConnectSession(null!, services, protocol, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>(() => new DirectConnectSession(Stream.Null, null!, protocol, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>(() => new DirectConnectSession(Stream.Null, services, null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>(() => new DirectConnectSession(Stream.Null, services, protocol, null!));
    }

    [Fact]
    public async Task HandshakeCompleted_RaisedOnRideOn()
    {
        using var pair = await LoopbackPair.CreateAsync();
        await using var session = NewSession(pair);
        var run = session.RunAsync();
        var raised = new TaskCompletionSource();
        session.HandshakeCompleted += (_, _) => raised.TrySetResult();
        var rideOn = System.Text.Encoding.ASCII.GetBytes("RideOn").Concat(new byte[] { 2, 4 }).ToArray();
        await pair.Client.WriteAsync(Req(DirectConnectMessageId.WriteCharacteristic, 1, Cat(ZwiftRideUuids.SyncRx.ToBytes(), rideOn)));
        await raised.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(session.IsHandshakeComplete);
    }
}
