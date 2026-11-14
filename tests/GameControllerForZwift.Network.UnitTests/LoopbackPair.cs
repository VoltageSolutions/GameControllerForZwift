using System.Net;
using System.Net.Sockets;
using GameControllerForZwift.Network.DirectConnect;

namespace GameControllerForZwift.Network.UnitTests;

/// <summary>Two connected streams over a loopback TCP connection on an ephemeral port (never 36866).</summary>
internal sealed class LoopbackPair : IDisposable
{
    private readonly TcpClient _serverSide;
    private readonly TcpClient _clientSide;

    private LoopbackPair(TcpClient serverSide, TcpClient clientSide)
    {
        _serverSide = serverSide;
        _clientSide = clientSide;
        Server = serverSide.GetStream();
        Client = clientSide.GetStream();
    }

    public Stream Server { get; }
    public Stream Client { get; }

    public static async Task<LoopbackPair> CreateAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var client = new TcpClient { NoDelay = true };
            var acceptTask = listener.AcceptTcpClientAsync();
            await client.ConnectAsync(IPAddress.Loopback, port);
            var server = await acceptTask;
            server.NoDelay = true;
            return new LoopbackPair(server, client);
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>Reads exactly <paramref name="count"/> DIRCON messages (10 s timeout).</summary>
    public async Task<List<DirectConnectMessage>> ReadMessagesAsync(int count)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = new List<DirectConnectMessage>();
        var buffer = new byte[256 * 1024];
        int length = 0;
        while (result.Count < count)
        {
            int read = await Client.ReadAsync(buffer.AsMemory(length), cts.Token);
            if (read == 0)
                break;
            length += read;
            int offset = 0;
            while (DirectConnectMessage.TryParse(buffer.AsSpan(offset, length - offset), out var msg, out int consumed))
            {
                result.Add(msg!);
                offset += consumed;
            }
            Buffer.BlockCopy(buffer, offset, buffer, 0, length - offset);
            length -= offset;
        }
        return result;
    }

    public void Dispose()
    {
        _clientSide.Dispose();
        _serverSide.Dispose();
    }
}
