using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Network.Dircon;

/// <summary>
/// Experimental raw TCP listener for the (undocumented) _zwift-protocol._tcp service. Logs every received chunk (hex + ASCII);
/// if the first bytes parse as a DirCon header the connection is handed to a normal <see cref="DirconSession"/>,
/// otherwise nothing is sent and the stream is only logged.
/// </summary>
public sealed class ZpListener : IAsyncDisposable
{
    private readonly DeviceIdentity _identity;
    private readonly DeviceProfileOptions _profile;
    private readonly ILogger _logger;
    private readonly int _port;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;

    public ZpListener(DeviceIdentity identity, DeviceProfileOptions profile, ILogger logger, int port)
    {
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _port = port;
    }

    /// <summary>Port actually bound.</summary>
    public int Port { get; private set; }

    public void Start()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _listener = listener;
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _cts = new CancellationTokenSource();
        _acceptTask = Task.Run(() => AcceptLoopAsync(listener, _cts.Token));
        _logger.LogInformation("ZP listener on TCP port {Port}", Port);
    }

    public async ValueTask DisposeAsync()
    {
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
        try { _listener?.Stop(); } catch (SocketException) { }
        if (_acceptTask != null)
        {
            try { await _acceptTask.ConfigureAwait(false); } catch (Exception) { }
        }
        _cts?.Dispose();
        _cts = null;
        _listener = null;
        _acceptTask = null;
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException ex)
            {
                if (ct.IsCancellationRequested) break;
                _logger.LogWarning(ex, "ZP accept failed");
                continue;
            }
            _ = Task.Run(() => HandleClientAsync(client, ct), CancellationToken.None);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogInformation("ZP accepted connection from {Remote}", remote);
        try
        {
            try { client.NoDelay = true; } catch (SocketException) { }
            var net = client.GetStream();
            var buf = new byte[4096];
            int total = 0;
            bool handedOff = false;
            var first = new List<byte>();

            // Read until we can classify the stream (>= 6 bytes or the peer stops sending).
            while (!ct.IsCancellationRequested)
            {
                int n = await net.ReadAsync(buf.AsMemory(), ct).ConfigureAwait(false);
                if (n == 0) break;
                total += n;
                LogChunk("RX", remote, buf.AsSpan(0, n));
                first.AddRange(buf.AsSpan(0, n).ToArray());
                if (first.Count < DirconMessage.HeaderSize && LooksLikeStart(first)) continue;
                break;
            }

            if (first.Count > 0)
            {
                var data = first.ToArray();
                if (data.Length >= 2 && data[0] == 0x16 && data[1] == 0x03)
                    _logger.LogInformation("ZP {Remote}: first bytes look like a TLS record (16 03 ...)", remote);
                else if (LooksLikeHttp(data))
                    _logger.LogInformation("ZP {Remote}: first bytes look like HTTP", remote);

                if (data.Length >= DirconMessage.HeaderSize
                    && data[0] == DirconMessage.CurrentVersion
                    && data[1] is >= DirconMessageId.DiscoverServices and <= DirconMessageId.Notification)
                {
                    _logger.LogInformation("ZP {Remote}: first frame header is valid DirCon (id=0x{Id:X2} seq={Seq} len={Len}); handing over to DirconSession",
                        remote, data[1], data[2], (data[4] << 8) | data[5]);
                    handedOff = true;
                    var protocol = new ZwiftControlProtocol(_identity, _logger, _profile.Controller);
                    var stream = new ZpStream(net, data, (dir, bytes) => LogChunk(dir, remote, bytes));
                    await using var session = new DirconSession(stream, DeviceProfiles.CreateServices(_profile, _identity), protocol, _logger, "zp:" + remote);
                    session.HandshakeCompleted += (_, _) => _logger.LogInformation("ZP {Remote}: RideOn handshake complete", remote);
                    await session.RunAsync(ct).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("ZP {Remote}: first bytes are not a DirCon frame; sending nothing, logging only", remote);
                }
            }

            if (!handedOff)
            {
                while (!ct.IsCancellationRequested)
                {
                    int n = await net.ReadAsync(buf.AsMemory(), ct).ConfigureAwait(false);
                    if (n == 0) break;
                    total += n;
                    LogChunk("RX", remote, buf.AsSpan(0, n));
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogInformation("ZP {Remote}: error {Type}: {Message}", remote, ex.GetType().Name, ex.Message);
        }
        finally
        {
            try { client.Dispose(); } catch (Exception ex) { _logger.LogDebug(ex, "ZP client dispose failed"); }
            _logger.LogInformation("ZP closed connection from {Remote}", remote);
        }
    }

    // A short first chunk (< 6 bytes) that could still be a DirCon header: keep reading.
    private static bool LooksLikeStart(List<byte> bytes) => bytes[0] == DirconMessage.CurrentVersion;

    private static bool LooksLikeHttp(byte[] d)
    {
        var s = Encoding.ASCII.GetString(d, 0, Math.Min(d.Length, 8));
        return s.StartsWith("GET ", StringComparison.Ordinal) || s.StartsWith("POST ", StringComparison.Ordinal)
            || s.StartsWith("PUT ", StringComparison.Ordinal) || s.StartsWith("HEAD ", StringComparison.Ordinal)
            || s.StartsWith("OPTIONS", StringComparison.Ordinal) || s.StartsWith("HTTP/", StringComparison.Ordinal);
    }

    private void LogChunk(string dir, string remote, ReadOnlySpan<byte> bytes)
    {
        var ascii = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
            ascii.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
        _logger.LogInformation("ZP {Dir} {Remote} {Time:HH:mm:ss.fff} {Len}B hex={Hex} ascii={Ascii}",
            dir, remote, DateTime.Now, bytes.Length, Convert.ToHexString(bytes), ascii.ToString());
    }

    /// <summary>Replays already-read bytes, then reads from the network; logs everything read and written afterwards.</summary>
    private sealed class ZpStream : Stream
    {
        private readonly NetworkStream _inner;
        private readonly Action<string, byte[]> _log;
        private byte[]? _prefix;
        private int _prefixPos;

        public ZpStream(NetworkStream inner, byte[] prefix, Action<string, byte[]> log)
        {
            _inner = inner; _prefix = prefix; _log = log;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_prefix != null)
            {
                int n = Math.Min(buffer.Length, _prefix.Length - _prefixPos);
                _prefix.AsMemory(_prefixPos, n).CopyTo(buffer);
                _prefixPos += n;
                if (_prefixPos >= _prefix.Length) _prefix = null;
                return n; // already logged by the listener
            }
            int read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read > 0) _log("RX", buffer.Span.Slice(0, read).ToArray());
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _log("TX", buffer.AsSpan(offset, count).ToArray());
            _inner.Write(buffer, offset, count);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _log("TX", buffer.ToArray());
            await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    }
}
