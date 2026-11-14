using GameControllerForZwift.Network.DirectConnect;
using GameControllerForZwift.Network.ZwiftRide;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>
/// The Zwift Ride profile (<see cref="ZwiftRideProfile"/> plus <see cref="ZwiftControlProtocol"/>) against the decoded loopback capture
/// docs/captures/BikeControl Loopback.pcapng (Zwift to BikeControl on TCP 36868, first 0.6 s of the conversation).
/// </summary>
public class ZwiftRideReplicaTests
{
    private static readonly byte[] Zwift2 = BleUuid.Parse("00000002-19CA-4651-86E5-FA29DCDD09D1").ToBytes();
    private static readonly byte[] Zwift4 = BleUuid.Parse("00000004-19CA-4651-86E5-FA29DCDD09D1").ToBytes();

    private static byte[] Hex(string h) => Convert.FromHexString(h);
    private static byte[] Cat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    // (message id, seq, request payload hex, captured response payload hex) in the order Zwift sent them, then the writes BikeControl never answered (ack only).
    private static readonly (byte Id, byte Seq, string Req, string Resp)[] Steps =
    {
        (1, 0, "", "0000fc8200001000800000805f9b34fbd273f680d548419db9d1fa04723452290000182600001000800000805f9b34fb0000180a00001000800000805f9b34fb0000000119ca465186e5fa29dcdd09d10000180d00001000800000805f9b34fb"),
        (2, 1, "0000fc8200001000800000805f9b34fb", "0000fc8200001000800000805f9b34fb0000000319ca465186e5fa29dcdd09d1020000000219ca465186e5fa29dcdd09d1040000000419ca465186e5fa29dcdd09d104"),
        (2, 2, "0000182600001000800000805f9b34fb", "0000182600001000800000805f9b34fb00002acc00001000800000805f9b34fb0100002ad200001000800000805f9b34fb0400002ad500001000800000805f9b34fb0100002ad600001000800000805f9b34fb0100002ad800001000800000805f9b34fb0100002ad900001000800000805f9b34fb0a00002ada00001000800000805f9b34fb0400002ad300001000800000805f9b34fb05"),
        (2, 3, "0000180a00001000800000805f9b34fb", "0000180a00001000800000805f9b34fb00002a2900001000800000805f9b34fb0100002a2500001000800000805f9b34fb0100002a2700001000800000805f9b34fb0100002a2600001000800000805f9b34fb01"),
        (2, 4, "0000000119ca465186e5fa29dcdd09d1", "0000000119ca465186e5fa29dcdd09d10000000219ca465186e5fa29dcdd09d1040000000319ca465186e5fa29dcdd09d1020000000419ca465186e5fa29dcdd09d108"),
        (2, 5, "0000180d00001000800000805f9b34fb", "0000180d00001000800000805f9b34fb00002a3700001000800000805f9b34fb0400002a3800001000800000805f9b34fb01"),
        (5, 6, "0000000219ca465186e5fa29dcdd09d101", "0000000219ca465186e5fa29dcdd09d1"),
        (5, 7, "0000000419ca465186e5fa29dcdd09d101", "0000000419ca465186e5fa29dcdd09d1"),
        (3, 8, "00002acc00001000800000805f9b34fb", "00002acc00001000800000805f9b34fb8a4000001e200000"),
        (5, 9, "00002ad200001000800000805f9b34fb01", "00002ad200001000800000805f9b34fb"),
        (5, 10, "00002ada00001000800000805f9b34fb01", "00002ada00001000800000805f9b34fb"),
        (3, 11, "00002a2900001000800000805f9b34fb", "00002a2900001000800000805f9b34fb42696b65436f6e74726f6c"),
        (3, 12, "00002a2500001000800000805f9b34fb", "00002a2500001000800000805f9b34fb31333337"),
        (3, 13, "00002a2700001000800000805f9b34fb", "00002a2700001000800000805f9b34fb312e30"),
        (3, 14, "00002a2600001000800000805f9b34fb", "00002a2600001000800000805f9b34fb312e30"),
        (5, 15, "0000000219ca465186e5fa29dcdd09d101", "0000000219ca465186e5fa29dcdd09d1"),
        (5, 16, "00002a3700001000800000805f9b34fb01", "00002a3700001000800000805f9b34fb"),
        (4, 17, "0000000319ca465186e5fa29dcdd09d1526964654f6e0204", "0000000319ca465186e5fa29dcdd09d1"),
        (4, 18, "0000000319ca465186e5fa29dcdd09d1000800", "0000000319ca465186e5fa29dcdd09d1"),
        (4, 19, "0000000319ca465186e5fa29dcdd09d1000810", "0000000319ca465186e5fa29dcdd09d1"),
        (4, 20, "0000000319ca465186e5fa29dcdd09d100088008", "0000000319ca465186e5fa29dcdd09d1"),
        (4, 21, "0000000319ca465186e5fa29dcdd09d100088306", "0000000319ca465186e5fa29dcdd09d1"),
        (4, 22, "0000000319ca465186e5fa29dcdd09d1410805", "0000000319ca465186e5fa29dcdd09d1"),
        (4, 23, "0000000319ca465186e5fa29dcdd09d1442001", "0000000319ca465186e5fa29dcdd09d1"),
        (4, 24, "0000000319ca465186e5fa29dcdd09d1440801", "0000000319ca465186e5fa29dcdd09d1"),
        (4, 25, "0000000319ca465186e5fa29dcdd09d10008800a", "0000000319ca465186e5fa29dcdd09d1"),
    };

    // Notifications BikeControl emitted for those requests, in order (RideOn reply first, then the status string, then the 0x3c replies).
    private static readonly (byte[] Uuid, string Data)[] Notifications =
    {
        (Zwift4, "526964654f6e0200"),
        (Zwift2, "2a0803120d220b524944455f4f4e28302900"),
        (Zwift4, "3c080012450a430883041204000001011a0a5a776966742052696465320f30422d3538443135414242343336333a03422e30420408021001480150085a0c353844313541424234333633"),
        (Zwift4, "3c081012020800"),
        (Zwift4, "3c088008120608ffff031003"),
        (Zwift4, "3c08830612080800106418002000"),
    };

    private static ZwiftControlProtocol NewProtocol() => new();

    [Fact]
    public async Task CapturedConversation_ProducesCapturedResponses()
    {
        using var pair = await LoopbackPair.CreateAsync();
        var session = new DirectConnectSession(pair.Server, ZwiftRideProfile.CreateServices(), NewProtocol(), NullLogger.Instance, "test");
        var run = session.RunAsync();

        var burst = Steps.SelectMany(s => DirectConnectMessage.Create(s.Id, s.Seq, 0, Hex(s.Req)).Encode()).ToArray();
        await pair.Client.WriteAsync(burst);

        var messages = await pair.ReadMessagesAsync(Steps.Length + Notifications.Length);
        Assert.Equal(Steps.Length + Notifications.Length, messages.Count);

        var responses = messages.Where(m => m.MessageId != DirectConnectMessageId.Notification).ToList();
        Assert.Equal(Steps.Length, responses.Count);
        for (int i = 0; i < Steps.Length; i++)
        {
            Assert.Equal(Steps[i].Id, responses[i].MessageId);
            Assert.Equal(Steps[i].Seq, responses[i].Seq);
            Assert.Equal((byte)0, responses[i].ResponseCode);
            Assert.Equal(Hex(Steps[i].Resp), responses[i].Payload);
        }

        var notes = messages.Where(m => m.MessageId == DirectConnectMessageId.Notification).ToList();
        Assert.Equal(Notifications.Length, notes.Count);
        for (int i = 0; i < notes.Count; i++)
            Assert.Equal(Cat(Notifications[i].Uuid, Hex(Notifications[i].Data)), notes[i].Payload);

        await session.DisposeAsync();
        await Task.WhenAny(run, Task.Delay(5000));
    }

    [Fact]
    public void UnansweredRequests_ProduceNothing()
    {
        var protocol = NewProtocol();
        foreach (var hex in new[] { "410805", "442001", "440801", "0008800a", "00089004", "3f080010022203288002" })
            Assert.Empty(protocol.HandleWrite(ZwiftRideUuids.SyncRx, Hex(hex)));
    }

    private static readonly string[] ServicePrefixes = ["0000FC82", "D273F680", "00001826", "0000180A", "00000001", "0000180D"];
    private static readonly string[] DeviceInfoValues = ["BikeControl", "1337", "1.0", "1.0"];

    [Fact]
    public void Gatt_HasBothZwiftInstancesAndCapturedProperties()
    {
        var services = ZwiftRideProfile.CreateServices();
        Assert.Equal(ServicePrefixes,
            services.Select(s => s.Uuid.ToString().Substring(0, 8).ToUpperInvariant()).ToArray());

        var fc82 = services[0];
        Assert.Equal(new[] { ZwiftRideUuids.SyncRx, ZwiftRideUuids.AsyncOut, ZwiftRideUuids.SyncTx }, fc82.Characteristics.Select(c => c.Uuid).ToArray());
        Assert.Equal(new[] { GattProperties.Write, GattProperties.Notify, GattProperties.Notify }, fc82.Characteristics.Select(c => c.Properties).ToArray());

        var zwift = services[4];
        Assert.Equal(ZwiftRideUuids.ZwiftService, zwift.Uuid);
        Assert.Equal(new[] { ZwiftRideUuids.AsyncOut, ZwiftRideUuids.SyncRx, ZwiftRideUuids.SyncTx }, zwift.Characteristics.Select(c => c.Uuid).ToArray());
        Assert.Equal(new[] { GattProperties.Notify, GattProperties.Write, GattProperties.Indicate }, zwift.Characteristics.Select(c => c.Properties).ToArray());

        var info = services[3].Characteristics.Select(c => System.Text.Encoding.ASCII.GetString(c.Value)).ToArray();
        Assert.Equal(DeviceInfoValues, info);
    }

    [Fact]
    public async Task ButtonFrame_IsSentOn00000002_AndMatchesTheIdleFrame()
    {
        // Idle keypad frame of the capture, every 5 s: 23 08 ff ff ff ff 0f.
        Assert.Equal(Hex("2308ffffffff0f"), ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.None));

        // Both 00000002 instances (FC82 and 00000001-19CA) exist in the model; DIRCON addresses them by UUID, so one notification reaches Zwift.
        var services = ZwiftRideProfile.CreateServices();
        Assert.Equal(2, services.SelectMany(s => s.Characteristics).Count(c => c.Uuid == ZwiftRideUuids.AsyncOut));

        using var pair = await LoopbackPair.CreateAsync();
        var session = new DirectConnectSession(pair.Server, services, NewProtocol(), NullLogger.Instance, "test");
        var run = session.RunAsync();
        await pair.Client.WriteAsync(DirectConnectMessage.Create(5, 1, 0, Cat(Zwift2, new byte[] { 1 })).Encode());
        var ack = await pair.ReadMessagesAsync(1);
        Assert.Equal(Zwift2, ack[0].Payload);

        Assert.True(await session.NotifyAsync(ZwiftRideUuids.AsyncOut, ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.None), false));
        var note = await pair.ReadMessagesAsync(1);
        Assert.Equal(Cat(Zwift2, Hex("2308ffffffff0f")), note[0].Payload);

        await session.DisposeAsync();
        await Task.WhenAny(run, Task.Delay(5000));
    }
}
