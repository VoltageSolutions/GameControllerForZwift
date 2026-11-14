using System.Net;
using System.Net.Sockets;
using GameControllerForZwift.Network.DirectConnect;
using GameControllerForZwift.Network.ZwiftRide;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>DirectConnectServer over real loopback sockets (ephemeral ports, never 36866).</summary>
public class DirectConnectServerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        try { return ((IPEndPoint)l.LocalEndpoint).Port; }
        finally { l.Stop(); }
    }

    private static DirectConnectServer NewServer(TimeSpan? keepAlive = null)
        => new(NullLogger<DirectConnectServer>.Instance, FreePort(), keepAlive ?? TimeSpan.FromHours(1));

    private static byte[] Cat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    private sealed class Client : IDisposable
    {
        private readonly TcpClient _tcp = new() { NoDelay = true };
        private readonly List<byte> _buffer = new();
        private NetworkStream _stream = null!;
        private byte _seq;

        public static async Task<Client> ConnectAsync(DirectConnectServer server)
        {
            var c = new Client();
            await c._tcp.ConnectAsync(IPAddress.Loopback, server.Port).WaitAsync(Timeout);
            c._stream = c._tcp.GetStream();
            return c;
        }

        public async Task SendAsync(byte id, byte[] payload)
            => await _stream.WriteAsync(DirectConnectMessage.Create(id, ++_seq, 0, payload).Encode());

        public Task SubscribeAsync(BleUuid uuid) => SendAsync(DirectConnectMessageId.EnableNotifications, Cat(uuid.ToBytes(), new byte[] { 1 }));

        public Task WriteAsync(BleUuid uuid, byte[] value) => SendAsync(DirectConnectMessageId.WriteCharacteristic, Cat(uuid.ToBytes(), value));

        /// <summary>Reads messages until <paramref name="match"/> returns true; returns that message.</summary>
        public async Task<DirectConnectMessage> ReadUntilAsync(Func<DirectConnectMessage, bool> match)
        {
            using var cts = new CancellationTokenSource(Timeout);
            var chunk = new byte[4096];
            while (true)
            {
                while (DirectConnectMessage.TryParse(_buffer.ToArray(), out var msg, out int consumed))
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

        public Task<DirectConnectMessage> ReadNotificationAsync(BleUuid uuid, Func<byte[], bool>? valueMatch = null)
            => ReadUntilAsync(m => m.MessageId == DirectConnectMessageId.Notification
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
        await client.SubscribeAsync(ZwiftRideUuids.AsyncOut);
        await client.SubscribeAsync(ZwiftRideUuids.SyncTx);
        await client.WriteAsync(ZwiftRideUuids.SyncRx, RideOn);
        await client.ReadNotificationAsync(ZwiftRideUuids.SyncTx);
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
            await using var server = new DirectConnectServer(NullLogger<DirectConnectServer>.Instance, busy, TimeSpan.FromHours(1));
            await server.StartAsync();
            Assert.InRange(server.Port, busy + 1, busy + 10);
        }
        finally { blocker.Stop(); }
    }

    [Fact]
    public void Constructor_RejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectConnectServer(null!));
    }

    [Fact]
    public async Task SendBeforeClientConnects_RemembersStateWithoutThrowing()
    {
        await using var server = NewServer();
        await server.StartAsync();
        await server.SendInputAsync(ZwiftRideButton.Up, 10, -10);
        await server.SendKeypadFrameAsync(new byte[] { 0x23 });
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
    public async Task SendInputAsync_SendsRideKeypadFrameWithAnalogPaddles_OnAsyncOut()
    {
        await using var server = NewServer();
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        await server.SendInputAsync(ZwiftRideButton.A, -40, 25);
        var expected = ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.A, new RideAnalogEntry(0, -40), new RideAnalogEntry(1, 25));
        var msg = await client.ReadNotificationAsync(ZwiftRideUuids.AsyncOut, v => v.SequenceEqual(expected));
        Assert.Equal(0, msg.Seq);

        await server.SendInputAsync(ZwiftRideButton.None, 0, 0);
        await client.ReadNotificationAsync(ZwiftRideUuids.AsyncOut, v => v.SequenceEqual(ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, 0), new RideAnalogEntry(1, 0))));
    }

    [Fact]
    public async Task BatteryReply_FollowsSetBatteryPercent_AndDefaultsTo100()
    {
        await using var server = NewServer();
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        server.SetBatteryPercent(42);
        await client.WriteAsync(ZwiftRideUuids.SyncRx, new byte[] { 0x00, 0x08, 0x83, 0x06 });
        await client.ReadNotificationAsync(ZwiftRideUuids.SyncTx, v => v.SequenceEqual(ZwiftControlProtocol.BuildBatteryReply(42)));

        server.SetBatteryPercent(null);
        await client.WriteAsync(ZwiftRideUuids.SyncRx, new byte[] { 0x00, 0x08, 0x83, 0x06 });
        await client.ReadNotificationAsync(ZwiftRideUuids.SyncTx, v => v.SequenceEqual(ZwiftControlProtocol.BuildBatteryReply(100)));
    }

    [Fact]
    public async Task KeepAlive_ResendsTheCurrentKeypadFramePeriodically()
    {
        await using var server = NewServer(TimeSpan.FromMilliseconds(40));
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        var idle = ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.None, new RideAnalogEntry(0, 0), new RideAnalogEntry(1, 0));
        await client.ReadNotificationAsync(ZwiftRideUuids.AsyncOut, v => v.SequenceEqual(idle));

        await server.SendInputAsync(ZwiftRideButton.Z, 0, 0);
        var held = ZwiftControlProtocol.BuildRideKeyPadFrame(ZwiftRideButton.Z, new RideAnalogEntry(0, 0), new RideAnalogEntry(1, 0));
        await client.ReadNotificationAsync(ZwiftRideUuids.AsyncOut, v => v.SequenceEqual(held));
        await client.ReadNotificationAsync(ZwiftRideUuids.AsyncOut, v => v.SequenceEqual(held)); // the keep-alive repeats the remembered state
    }

    [Fact]
    public async Task SendKeypadFrameAsync_SendsTheRawFrame()
    {
        await using var server = NewServer();
        await server.StartAsync();
        using var client = await Client.ConnectAsync(server);
        await HandshakeAsync(client);
        await WaitUntilAsync(() => server.IsZwiftConnected);

        var frame = ZwiftControlProtocol.BuildKeyPadFrame(ZwiftRideButton.B);
        await server.SendKeypadFrameAsync(frame);
        await client.ReadNotificationAsync(ZwiftRideUuids.AsyncOut, v => v.SequenceEqual(frame));
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
        await second.ReadNotificationAsync(ZwiftRideUuids.AsyncOut, v => v.Length > 0 && v[0] == 0x23);
        Assert.True(server.IsZwiftConnected);
    }
}
