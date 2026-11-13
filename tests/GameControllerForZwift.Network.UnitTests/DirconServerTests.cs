using System.Net;
using System.Net.Sockets;
using GameControllerForZwift.Network.Dircon;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>DirconServer over real loopback sockets (ephemeral ports, never 36866).</summary>
public class DirconServerTests
{
    private static readonly DeviceIdentity Identity = new("KICKR BIKE SHIFT 0001", "100000001", "02-47-5A-00-00-01", "1.21.18", "3");
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        try { return ((IPEndPoint)l.LocalEndpoint).Port; }
        finally { l.Stop(); }
    }

    private static DirconServer NewServer(DeviceProfileOptions? profile = null, TimeSpan? interval = null)
        => new(Identity, NullLogger<DirconServer>.Instance, FreePort(), interval ?? TimeSpan.FromHours(1), profile);

    private static byte[] Cat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    private sealed class Client : IDisposable
    {
        private readonly TcpClient _tcp = new() { NoDelay = true };
        private readonly List<byte> _buffer = new();
        private NetworkStream _stream = null!;
        private byte _seq;

        public static async Task<Client> ConnectAsync(DirconServer server)
        {
            var c = new Client();
            await c._tcp.ConnectAsync(IPAddress.Loopback, server.Port).WaitAsync(Timeout);
            c._stream = c._tcp.GetStream();
            return c;
        }

        public async Task SendAsync(byte id, byte[] payload)
            => await _stream.WriteAsync(DirconMessage.Create(id, ++_seq, 0, payload).Encode());

        public Task SubscribeAsync(BleUuid uuid) => SendAsync(DirconMessageId.EnableNotifications, Cat(uuid.ToBytes(), new byte[] { 1 }));

        public Task WriteAsync(BleUuid uuid, byte[] value) => SendAsync(DirconMessageId.WriteCharacteristic, Cat(uuid.ToBytes(), value));

        /// <summary>Reads messages until <paramref name="match"/> returns true; returns that message.</summary>
        public async Task<DirconMessage> ReadUntilAsync(Func<DirconMessage, bool> match)
        {
            using var cts = new CancellationTokenSource(Timeout);
            var chunk = new byte[4096];
            while (true)
            {
                while (DirconMessage.TryParse(_buffer.ToArray(), out var msg, out int consumed))
                {
                    _buffer.RemoveRange(0, consumed);
                    if (match(msg!))
                        return msg!;
                }
                int n = await _stream.ReadAsync(chunk, cts.Token);
                if (n == 0)
                    throw new EndOfStreamException();
                _buffer.AddRange(chunk.AsSpan(0, n).ToArray());
            }
        }

        public Task<DirconMessage> ReadNotificationAsync(BleUuid uuid, Func<byte[], bool>? valueMatch = null)
            => ReadUntilAsync(m => m.MessageId == DirconMessageId.Notification
                && BleUuid.FromBytes(m.Payload.AsSpan(0, 16)) == uuid
                && (valueMatch?.Invoke(m.Payload[16..]) ?? true));

        public void Dispose() => _tcp.Dispose();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException();
            await Task.Delay(10);
        }
    }

    private static readonly byte[] RideOn = System.Text.Encoding.ASCII.GetBytes("RideOn").Concat(new byte[] { 2, 4 }).ToArray();

    private static async Task HandshakeAsync(Client client)
    {
        await client.SubscribeAsync(DirconUuids.ZwiftAsync);
        await client.SubscribeAsync(DirconUuids.ZwiftSyncTx);
        await client.WriteAsync(DirconUuids.ZwiftSyncRx, RideOn);
        await client.ReadNotificationAsync(DirconUuids.ZwiftSyncTx);
    }

    [Fact]
    public async Task StartAndStop_BindsPort_AndIsIdempotent()
    {
        await using var server = NewServer();
        await server.StartAsync();
        await server.StartAsync();
        Assert.True(server.Port > 0);
        Assert.False(server.IsZwiftConnected);
        using var client = await Client.ConnectAsync(server);
        await server.StopAsync();
        await server.StopAsync();
    }

    [Fact]
    public async Task StopWithoutStart_DoesNothing()
    {
        await using var server = NewServer();
        await server.StopAsync();
    }

    [Fact]
    public async Task BusyPort_FallsThroughToNext()
    {
        int busy = FreePort();
        var blocker = Socket.OSSupportsIPv6 ? new TcpListener(IPAddress.IPv6Any, busy) : new TcpListener(IPAddress.Any, busy);
        if (Socket.OSSupportsIPv6)
            blocker.Server.DualMode = true;
        blocker.Start();
        try
        {
            await using var server = new DirconServer(Identity, NullLogger<DirconServer>.Instance, busy, TimeSpan.FromHours(1));
            await server.StartAsync();
            Assert.InRange(server.Port, busy + 1, busy + 10);
        }
        finally { blocker.Stop(); }
    }

    [Fact]
    public void Constructor_RejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => new DirconServer(null!, NullLogger<DirconServer>.Instance));
        Assert.Throws<ArgumentNullException>(() => new DirconServer(Identity, null!));
    }

    [Fact]
    public async Task SendBeforeClientConnects_RemembersStateWithoutThrowing()
    {
        await using var server = NewServer();
        await server.StartAsync();
        await server.SendInputAsync(ZwiftRideButton.Up, 10, -10);
        await server.SendButtonsAsync(ZwiftRideButton.A);
        await server.SendGearAsync(3);
        await server.SendAsyncFrameAsync(new byte[] { 0x37 });
    }

    [Fact]
    public async Task Handshake_SetsConnected_RaisesEvents_AndDisconnectClears()
    {
        await using var server = NewServer();
        var changes = new List<bool>();
        server.ConnectionChanged += (_, v) => { lock (changes) changes.Add(v); };
        await server.StartAsync();

        var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        client.Dispose();
        await WaitUntilAsync(() => !server.IsZwiftConnected);
        lock (changes)
            Assert.Equal(new[] { true, false }, changes);
    }

    [Fact]
    public async Task SendInputAsync_SendsKeypadFrameOnZwiftAsync()
    {
        await using var server = NewServer();
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        await server.SendInputAsync(ZwiftRideButton.Z | ZwiftRideButton.Up, 0, 0);
        var expected = ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.Z | ZwiftRideButton.Up);
        var msg = await client.ReadNotificationAsync(DirconUuids.ZwiftAsync, v => v.SequenceEqual(expected));
        Assert.Equal(0, msg.Seq);

        await server.SendButtonsAsync(ZwiftRideButton.None);
        await client.ReadNotificationAsync(DirconUuids.ZwiftAsync, v => v.SequenceEqual(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.None)));
    }

    [Fact]
    public async Task BikeControlRideIdentity_SendsRideKeypadFrameWithAnalogPaddles_AndBatteryReply()
    {
        var profile = new DeviceProfileOptions(Gatt: GattKind.BikeControlRide, Controller: ControllerIdentity.BikeControlRide);
        await using var server = NewServer(profile);
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        await server.SendInputAsync(ZwiftRideButton.A, -40, 25);
        var expected = ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.A, new RideAnalogEntry(0, -40), new RideAnalogEntry(1, 25));
        await client.ReadNotificationAsync(DirconUuids.ZwiftAsync, v => v.SequenceEqual(expected));

        server.SetBatteryPercent(42);
        await client.WriteAsync(DirconUuids.ZwiftSyncRx, new byte[] { 0x00, 0x08, 0x83, 0x06 });
        await client.ReadNotificationAsync(DirconUuids.ZwiftSyncTx, v => v.SequenceEqual(ZwiftControlProtocol.BuildBatteryReply(42)));

        server.SetBatteryPercent(null);
        await client.WriteAsync(DirconUuids.ZwiftSyncRx, new byte[] { 0x00, 0x08, 0x83, 0x06 });
        await client.ReadNotificationAsync(DirconUuids.ZwiftSyncTx, v => v.SequenceEqual(ZwiftControlProtocol.BuildBatteryReply(100)));
    }

    [Fact]
    public async Task SendGearAsync_SendsChainringThenRearCog()
    {
        await using var server = NewServer();
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        await server.SendGearAsync(rearCog: 4, frontChainring: 1);
        await client.ReadNotificationAsync(DirconUuids.ZwiftAsync, v => v.SequenceEqual(ZwiftControlProtocol.BuildChainringFrame(1)));
        await client.ReadNotificationAsync(DirconUuids.ZwiftAsync, v => v.SequenceEqual(ZwiftControlProtocol.BuildRearCogFrame(4)));

        var click = ZwiftControlProtocol.BuildClickKeyPadFrame(true, false);
        await server.SendAsyncFrameAsync(click);
        await client.ReadNotificationAsync(DirconUuids.ZwiftAsync, v => v.SequenceEqual(click));
    }

    [Fact]
    public async Task SendGearAsync_WhenGearingDisabled_SendsNothing()
    {
        var profile = new DeviceProfileOptions(EnableWahooGearing: false);
        await using var server = NewServer(profile);
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        await server.SendGearAsync(4);
        var marker = ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.B);
        await server.SendInputAsync(ZwiftRideButton.B, 0, 0);
        // The very next notification on ZwiftAsync (after the ATX status strings) must be the marker, not a gear frame.
        var msg = await client.ReadNotificationAsync(DirconUuids.ZwiftAsync, v => v.Length > 0 && v[0] != 0x2A);
        Assert.Equal(marker, msg.Payload[16..]);
    }

    [Fact]
    public async Task SecondClient_ReplacesFirst()
    {
        await using var server = NewServer();
        await server.StartAsync();
        var first = await Client.ConnectAsync(server);
        await HandshakeAsync(first);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        using var second = await Client.ConnectAsync(server);
        await HandshakeAsync(second);
        await Assert.ThrowsAnyAsync<Exception>(() => first.ReadUntilAsync(_ => false));
        first.Dispose();

        await server.SendInputAsync(ZwiftRideButton.Left, 0, 0);
        await second.ReadNotificationAsync(DirconUuids.ZwiftAsync, v => v.SequenceEqual(ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.Left)));
        Assert.True(server.IsZwiftConnected);
    }

    [Fact]
    public async Task IdleNotifications_AreSentPeriodically()
    {
        await using var server = NewServer(interval: TimeSpan.FromMilliseconds(40));
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await client.SubscribeAsync(DirconUuids.IndoorBikeData);
        await client.SubscribeAsync(DirconUuids.CyclingPowerMeasurement);
        await client.SubscribeAsync(DirconUuids.WahooE03C);
        await client.ReadNotificationAsync(DirconUuids.IndoorBikeData, v => v.SequenceEqual(KickrBikeProfile.IdleIndoorBikeData));
        await client.ReadNotificationAsync(DirconUuids.CyclingPowerMeasurement, v => v.SequenceEqual(KickrBikeProfile.IdlePowerMeasurement));
        await client.ReadNotificationAsync(DirconUuids.WahooE03C, v => v.SequenceEqual(KickrBikeProfile.IdleE03C));
    }

    [Fact]
    public async Task SimulatedRide_SendsRideDataAtFourHertz_AndStops()
    {
        await using var server = NewServer(interval: TimeSpan.FromMilliseconds(40));
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await client.SubscribeAsync(DirconUuids.IndoorBikeData);
        await client.SubscribeAsync(DirconUuids.CyclingPowerMeasurement);
        await client.SubscribeAsync(DirconUuids.ZwiftAsync);
        await client.SubscribeAsync(DirconUuids.WahooE03C);

        Assert.False(server.IsRideSimulated);
        server.SetSimulatedRide(250, 90);
        Assert.True(server.IsRideSimulated);

        var bike = await client.ReadNotificationAsync(DirconUuids.IndoorBikeData, v => !v.SequenceEqual(KickrBikeProfile.IdleIndoorBikeData));
        Assert.NotEmpty(bike.Payload);
        await client.ReadNotificationAsync(DirconUuids.CyclingPowerMeasurement, v => !v.SequenceEqual(KickrBikeProfile.IdlePowerMeasurement));
        await client.ReadNotificationAsync(DirconUuids.WahooE03C);

        server.StopSimulatedRide();
        Assert.False(server.IsRideSimulated);
    }

    [Fact]
    public async Task SimulatedRide_InBikeControlReplica_SendsIndoorBikeAndHeartRate()
    {
        var profile = new DeviceProfileOptions(Gatt: GattKind.BikeControlRide, Controller: ControllerIdentity.BikeControlRide, AddPowerService: true);
        await using var server = NewServer(profile, TimeSpan.FromMilliseconds(40));
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await client.SubscribeAsync(DirconUuids.IndoorBikeData);
        await client.SubscribeAsync(DirconUuids.CyclingPowerMeasurement);
        await client.SubscribeAsync(BleUuid.FromShort(0x2A37));
        server.SetSimulatedRide(-5, -5); // negative values are clamped to 0
        await client.ReadNotificationAsync(DirconUuids.IndoorBikeData);
        await client.ReadNotificationAsync(DirconUuids.CyclingPowerMeasurement);
        await client.ReadNotificationAsync(BleUuid.FromShort(0x2A37), v => v.SequenceEqual(new byte[] { 0x00, 120 }));
    }
}
