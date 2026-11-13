using GameControllerForZwift.Network.Dircon;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>
/// Replays the Zwift to bike requests of docs/captures/kickr-bike-capture-analysis.md against a DirconSession
/// and compares the responses with the captured bike responses.
/// </summary>
public class DirconSessionReplayTests
{
    private static readonly DeviceIdentity CaptureIdentity =
        new("KICKR BIKE SHIFT 0001", "100000001", "02-47-5A-00-00-01", "1.21.18", "3");

    private const string WahooBase = "-0A7D-4AB3-97FA-F1500F9FEB8B";
    private const string ZwiftBase = "-19CA-4651-86E5-FA29DCDD09D1";

    private static byte[] Hex(string h) => Convert.FromHexString(h);
    private static byte[] Short(ushort u) => BleUuid.FromShort(u).ToBytes();
    private static byte[] Wahoo(string four) => BleUuid.Parse("A026" + four + WahooBase).ToBytes();
    private static byte[] Zwift(int n) => BleUuid.Parse($"{n:X8}{ZwiftBase}").ToBytes();
    private static byte[] Cat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
    private static byte[] Prop(byte[] uuid, byte p) => Cat(uuid, new[] { p });

    private static byte[] Req(byte id, byte seq, byte[] payload) => DirconMessage.Create(id, seq, 0, payload).Encode();

    private static readonly byte[] SvcDeviceInfo = Short(0x180A);
    private static readonly byte[] SvcPower = Short(0x1818);
    private static readonly byte[] SvcFtms = Short(0x1826);
    private static readonly byte[] SvcUserData = Short(0x181C);
    private static readonly byte[] SvcEe0D = Wahoo("EE0D");
    private static readonly byte[] SvcEe0B = Wahoo("EE0B");
    private static readonly byte[] SvcZwift = Zwift(1);

    private static readonly byte[] Zwift2 = Zwift(2);
    private static readonly byte[] Zwift3 = Zwift(3);
    private static readonly byte[] Zwift4 = Zwift(4);
    private static readonly byte[] E037 = Wahoo("E037");
    private static readonly byte[] E03C = Wahoo("E03C");

    private static readonly byte[] RideOn0202 = Hex("526964654f6e0202");
    private static readonly byte[] RideOnStatus = Hex("2a0803120d220b524944455f4f4e28332900");
    private static readonly byte[] Atx0 = Hex("2a08031211220f4154582030382c2053545820303000");
    private static readonly byte[] Atx1 = Hex("2a08031211220f4154582030382c2053545820303800");

    private static (DirconSession session, ZwiftControlProtocol protocol) NewSession(LoopbackPair pair)
    {
        var protocol = new ZwiftControlProtocol(CaptureIdentity);
        var session = new DirconSession(pair.Server, KickrBikeProfile.Create(CaptureIdentity), protocol, NullLogger.Instance, "test");
        return (session, protocol);
    }

    private static void AssertMsg(DirconMessage m, byte id, byte seq, byte rc, byte[] payload)
    {
        Assert.Equal(id, m.MessageId);
        Assert.Equal(seq, m.Seq);
        Assert.Equal(rc, m.ResponseCode);
        Assert.Equal(payload, m.Payload);
    }

    [Fact]
    public async Task CapturedConversation_ProducesCapturedResponses()
    {
        // (id, seq, request payload, expected response payload)
        var steps = new List<(byte Id, byte Seq, byte[] Req, byte[] Resp)>
        {
            (1, 0, Array.Empty<byte>(), Cat(SvcDeviceInfo, Wahoo("EE01"), SvcPower, SvcFtms, SvcUserData, SvcEe0D, SvcEe0B, Wahoo("EE06"), SvcZwift)),
            (2, 1, SvcDeviceInfo, Cat(SvcDeviceInfo, Prop(Short(0x2A29), 1), Prop(Short(0x2A25), 1), Prop(Short(0x2A27), 1), Prop(Short(0x2A26), 1))),
            (2, 2, SvcPower, Cat(SvcPower, Prop(Short(0x2A63), 4), Prop(Short(0x2A65), 1), Prop(Short(0x2A5D), 1), Prop(Short(0x2A66), 6))),
            (2, 3, SvcFtms, Cat(SvcFtms, Prop(Short(0x2ACC), 1), Prop(Short(0x2AD9), 6), Prop(Short(0x2AD2), 4), Prop(Short(0x2ADA), 4), Prop(Short(0x2AD8), 1))),
            (2, 4, SvcUserData, Cat(SvcUserData, Prop(Short(0x2A98), 3))),
            (2, 5, SvcEe0D, Cat(SvcEe0D, Prop(Wahoo("E039"), 6), Prop(Wahoo("E03A"), 4), Prop(E03C, 4))),
            (2, 6, SvcEe0B, Cat(SvcEe0B, Prop(E037, 6))),
            (2, 7, SvcZwift, Cat(SvcZwift, Prop(Zwift2, 4), Prop(Zwift3, 2), Prop(Zwift4, 4))),

            (3, 8, Short(0x2A29), Cat(Short(0x2A29), Hex("5761686f6f204669746e657373"))),
            (3, 9, Short(0x2A25), Cat(Short(0x2A25), Hex("313030303030303031"))),
            (3, 10, Short(0x2A27), Cat(Short(0x2A27), Hex("33"))),
            (3, 11, Short(0x2A26), Cat(Short(0x2A26), Hex("312e32312e3138"))),
            (5, 12, Cat(Short(0x2A63), new byte[] { 1 }), Short(0x2A63)),
            (3, 13, Short(0x2ACC), Cat(Short(0x2ACC), Hex("0240000008600000"))),
            (5, 14, Cat(Short(0x2AD9), new byte[] { 1 }), Short(0x2AD9)),
            (5, 15, Cat(Short(0x2AD2), new byte[] { 1 }), Short(0x2AD2)),
            (5, 16, Cat(Short(0x2ADA), new byte[] { 1 }), Short(0x2ADA)),
            (3, 17, Short(0x2A98), Cat(Short(0x2A98), Hex("023e"))),
            (5, 18, Cat(E03C, new byte[] { 1 }), E03C),
            (5, 19, Cat(E037, new byte[] { 1 }), E037),
            (5, 20, Cat(Zwift2, new byte[] { 1 }), Zwift2),
            (5, 21, Cat(Zwift4, new byte[] { 1 }), Zwift4),
            (4, 22, Cat(Zwift3, Hex("526964654f6e0203")), Zwift3),
            (4, 23, Cat(E037, Hex("33")), E037),
            (4, 24, Cat(Zwift3, Hex("526964654f6e0203")), Zwift3),
            (4, 25, Cat(Zwift3, Hex("410805")), Zwift3),
            (4, 26, Cat(Zwift3, Hex("000800")), Zwift3),
            (4, 27, Cat(Zwift3, Hex("000810")), Zwift3),
            (4, 28, Cat(Zwift3, Hex("3f08011000220c40ec2750e80558813e609003")), Zwift3),
            (4, 29, Cat(Zwift3, Hex("3f08011001220528a4023800")), Zwift3),
            (4, 30, Cat(Zwift3, Hex("00088008")), Zwift3),
            (4, 31, Cat(Zwift3, Hex("00088306")), Zwift3),
            (4, 32, Cat(Zwift3, Hex("00089004")), Zwift3),
            (4, 33, Cat(Zwift3, Hex("3f080010022203288002")), Zwift3),
        };

        // Expected notifications, in the order this implementation emits them.
        var notifications = new List<(byte[] Uuid, byte[] Data)>
        {
            (Zwift2, Atx0), (Zwift2, Atx1),                        // after enabling 00000002
            (Zwift2, RideOnStatus), (Zwift4, RideOn0202),          // RideOn seq 22
            (E037, Hex("fe33012000000020000000")),                 // 0x33 on E037
            (Zwift2, RideOnStatus), (Zwift4, RideOn0202),          // RideOn seq 24 (retry)
            (Zwift4, Hex("3e08411002")),                           // 41 08 05
            (Zwift4, Hex("3c080012390a370882041204120015011a0c4b49434b522042494b452053320f3434313030303030303031000000003a0133420408011024420408021000")),
            // 00 08 10 -> no reply
            (Zwift4, Hex("4008001000")),
            (Zwift4, Hex("4008001001")),
            (Zwift4, Hex("3c088008121d08d80110031a0a080110ffff0318feff031a0a080210ffff0318feff03")),
            (Zwift4, Hex("3c088306")),
            (Zwift4, Hex("3c089004120408012003")),
            // 3f with f1 == 0 -> no reply
        };

        using var pair = await LoopbackPair.CreateAsync();
        var (session, protocol) = NewSession(pair);
        var run = session.RunAsync();

        // Zwift pipelines everything: send the whole burst in one segment.
        var burst = steps.SelectMany(s => Req(s.Id, s.Seq, s.Req)).ToArray();
        await pair.Client.WriteAsync(burst);

        var messages = await pair.ReadMessagesAsync(steps.Count + notifications.Count);
        Assert.Equal(steps.Count + notifications.Count, messages.Count);

        var responses = messages.Where(m => m.MessageId != DirconMessageId.Notification).ToList();
        Assert.Equal(steps.Count, responses.Count);
        for (int i = 0; i < steps.Count; i++)
            AssertMsg(responses[i], steps[i].Id, steps[i].Seq, 0, steps[i].Resp);

        var notes = messages.Where(m => m.MessageId == DirconMessageId.Notification).ToList();
        Assert.Equal(notifications.Count, notes.Count);
        for (int i = 0; i < notes.Count; i++)
            AssertMsg(notes[i], DirconMessageId.Notification, 0, 0, Cat(notifications[i].Uuid, notifications[i].Data));

        // "RideOn 02 02" on 0004 arrives only after 0004 was enabled (seq 21 response precedes it).
        int enable0004 = messages.FindIndex(m => m.MessageId == 5 && m.Seq == 21);
        int rideOnReply = messages.FindIndex(m => m.MessageId == 6 && m.Payload.AsSpan(0, 16).SequenceEqual(Zwift4) && m.Payload.AsSpan(16).SequenceEqual(RideOn0202));
        Assert.True(enable0004 >= 0 && rideOnReply > enable0004);

        Assert.True(protocol.RideOnCompleted);
        Assert.True(session.IsHandshakeComplete);
        Assert.True(session.IsSubscribed(BleUuid.FromBytes(Zwift4)));

        await session.DisposeAsync();
        await Task.WhenAny(run, Task.Delay(5000));
    }

    [Fact]
    public async Task RideOnBeforeSubscription_IsQueuedUntilEnable()
    {
        using var pair = await LoopbackPair.CreateAsync();
        var (session, _) = NewSession(pair);
        var run = session.RunAsync();

        var burst = Cat(
            Req(4, 0, Cat(Zwift3, Hex("526964654f6e0203"))),
            Req(5, 1, Cat(Zwift4, new byte[] { 1 })),
            Req(5, 2, Cat(Zwift2, new byte[] { 1 })));
        await pair.Client.WriteAsync(burst);

        var m = await pair.ReadMessagesAsync(3 + 4);
        Assert.Equal(7, m.Count);
        AssertMsg(m[0], 4, 0, 0, Zwift3);
        AssertMsg(m[1], 5, 1, 0, Zwift4);
        AssertMsg(m[2], 6, 0, 0, Cat(Zwift4, RideOn0202));       // flushed right after the 0004 enable
        AssertMsg(m[3], 5, 2, 0, Zwift2);
        AssertMsg(m[4], 6, 0, 0, Cat(Zwift2, RideOnStatus));      // queued status string
        AssertMsg(m[5], 6, 0, 0, Cat(Zwift2, Atx0));
        AssertMsg(m[6], 6, 0, 0, Cat(Zwift2, Atx1));

        await session.DisposeAsync();
        await Task.WhenAny(run, Task.Delay(5000));
    }

    [Fact]
    public async Task NotifyAsync_IsNotSentWithoutSubscription_AndNotQueuedWhenDropped()
    {
        using var pair = await LoopbackPair.CreateAsync();
        var (session, _) = NewSession(pair);
        var run = session.RunAsync();

        bool sent = await session.NotifyAsync(BleUuid.FromBytes(Zwift2), new byte[] { 0x23 }, queueIfNotSubscribed: false);
        Assert.False(sent);

        await pair.Client.WriteAsync(Req(5, 0, Cat(Zwift2, new byte[] { 1 })));
        var m = await pair.ReadMessagesAsync(1 + 2);   // ack + two ATX status strings, nothing queued
        AssertMsg(m[0], 5, 0, 0, Zwift2);
        Assert.Equal(3, m.Count);

        Assert.True(await session.NotifyAsync(BleUuid.FromBytes(Zwift2), new byte[] { 0x23 }, false));
        var n = await pair.ReadMessagesAsync(1);
        AssertMsg(n[0], 6, 0, 0, Cat(Zwift2, new byte[] { 0x23 }));

        await session.DisposeAsync();
        await Task.WhenAny(run, Task.Delay(5000));
    }

    [Fact]
    public async Task ErrorCodes()
    {
        using var pair = await LoopbackPair.CreateAsync();
        var (session, _) = NewSession(pair);
        var run = session.RunAsync();

        var burst = Cat(
            Req(2, 1, Short(0x1234)),                    // unknown service -> 3
            Req(3, 2, Short(0x1234)),                    // unknown characteristic -> 4
            Req(4, 3, Cat(Short(0x2A29), Hex("00"))),    // write to read-only -> 5
            Req(5, 4, Cat(Short(0x2A29), new byte[] { 1 })), // notify on read-only -> 5
            Req(0x42, 5, Array.Empty<byte>()));          // unknown message -> 2
        await pair.Client.WriteAsync(burst);

        var m = await pair.ReadMessagesAsync(5);
        AssertMsg(m[0], 2, 1, 3, Array.Empty<byte>());
        AssertMsg(m[1], 3, 2, 4, Array.Empty<byte>());
        AssertMsg(m[2], 4, 3, 5, Array.Empty<byte>());
        AssertMsg(m[3], 5, 4, 5, Array.Empty<byte>());
        AssertMsg(m[4], 0x42, 5, 2, Array.Empty<byte>());

        await session.DisposeAsync();
        await Task.WhenAny(run, Task.Delay(5000));
    }

    [Fact]
    public async Task FragmentedRequest_IsReassembled()
    {
        using var pair = await LoopbackPair.CreateAsync();
        var (session, _) = NewSession(pair);
        var run = session.RunAsync();

        var frame = Req(3, 7, Short(0x2A29));
        for (int i = 0; i < frame.Length; i++)
        {
            await pair.Client.WriteAsync(frame.AsMemory(i, 1));
            await pair.Client.FlushAsync();
        }

        var m = await pair.ReadMessagesAsync(1);
        AssertMsg(m[0], 3, 7, 0, Cat(Short(0x2A29), Hex("5761686f6f204669746e657373")));

        await session.DisposeAsync();
        await Task.WhenAny(run, Task.Delay(5000));
    }
}
