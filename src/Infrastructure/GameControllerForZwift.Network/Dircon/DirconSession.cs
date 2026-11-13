using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Network.Dircon;

/// <summary>
/// One DIRCON client connection. Owns a <see cref="Stream"/> (any stream, so it is unit-testable),
/// reassembles pipelined requests and answers them per the DIRCON spec.
/// </summary>
public sealed class DirconSession : IAsyncDisposable
{
    private const int MaxPendingPerCharacteristic = 64;

    private readonly Stream _stream;
    private readonly IReadOnlyList<GattService> _services;
    private readonly ZwiftControlProtocol _protocol;
    private readonly ILogger _logger;
    private readonly string _name;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly object _gate = new();
    private readonly HashSet<BleUuid> _subscribed = new();
    private readonly Dictionary<BleUuid, Queue<byte[]>> _pending = new();
    private readonly CancellationTokenSource _cts = new();
    private int _disposed;
    private readonly Dictionary<BleUuid, int> _notifyCounters = new();

    private byte[] _buffer = new byte[4096];
    private int _bufferLength;

    /// <summary>Creates a session over <paramref name="stream"/>.</summary>
    public DirconSession(
        Stream stream,
        IReadOnlyList<GattService> services,
        ZwiftControlProtocol protocol,
        ILogger logger,
        string name = "client")
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _name = name;
        _protocol.RideOnHandled += (_, _) => HandshakeCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>True once the RideOn handshake has been answered.</summary>
    public bool IsHandshakeComplete => _protocol.RideOnCompleted;

    /// <summary>Raised when the RideOn handshake completes.</summary>
    public event EventHandler? HandshakeCompleted;

    /// <summary>True if the client has enabled notifications on <paramref name="characteristic"/>.</summary>
    public bool IsSubscribed(BleUuid characteristic)
    {
        lock (_gate)
            return _subscribed.Contains(characteristic);
    }

    /// <summary>Reads and answers requests until the stream ends or the token is cancelled.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        var ct = linked.Token;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (_bufferLength == _buffer.Length)
                    Array.Resize(ref _buffer, _buffer.Length * 2);

                int read = await _stream.ReadAsync(_buffer.AsMemory(_bufferLength), ct).ConfigureAwait(false);
                if (read == 0)
                    break;
                _bufferLength += read;

                int offset = 0;
                while (DirconMessage.TryParse(_buffer.AsSpan(offset, _bufferLength - offset), out var msg, out int consumed))
                {
                    offset += consumed;
                    await HandleMessageAsync(msg!, ct).ConfigureAwait(false);
                }

                if (offset > 0)
                {
                    Buffer.BlockCopy(_buffer, offset, _buffer, 0, _bufferLength - offset);
                    _bufferLength -= offset;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "DIRCON session {Name} I/O ended", _name);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (System.Net.Sockets.SocketException ex)
        {
            _logger.LogDebug(ex, "DIRCON session {Name} socket ended", _name);
        }
    }

    /// <summary>
    /// Sends a notification (msgId 6, seq 0) for <paramref name="characteristic"/>.
    /// If the client has not subscribed yet, it is queued (queueIfNotSubscribed) and flushed on subscription, or dropped.
    /// Returns true if it was sent immediately.
    /// </summary>
    public async Task<bool> NotifyAsync(BleUuid characteristic, byte[] data, bool queueIfNotSubscribed, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_subscribed.Contains(characteristic))
            {
                if (queueIfNotSubscribed)
                {
                    if (!_pending.TryGetValue(characteristic, out var q))
                        _pending[characteristic] = q = new Queue<byte[]>();
                    if (q.Count >= MaxPendingPerCharacteristic)
                        q.Dequeue();
                    q.Enqueue(data);
                }
                return false;
            }
        }
        await SendNotificationFrameAsync(characteristic, data, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private Task SendNotificationFrameAsync(BleUuid characteristic, byte[] data, CancellationToken ct)
    {
        if (characteristic == DirconUuids.ZwiftAsync && data.Length > 0 && data[0] == 0x23 && _logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("TX 0x23 keypad {Hex} => {Decoded}", Convert.ToHexString(data), ZwiftControlProtocol.DescribeKeyPadFrame(data));
        if (_logger.IsEnabled(LogLevel.Debug) && ShouldLogNotification(characteristic, data))
            _logger.LogDebug("DIRCON {Name} TX {MsgId} seq=0 resp=0 uuid={Uuid} value={Value}", _name, "Notification(0x06)", characteristic, Convert.ToHexString(data));
        var payload = new byte[16 + data.Length];
        characteristic.WriteTo(payload);
        data.CopyTo(payload, 16);
        return SendAsync(DirconMessage.Create(DirconMessageId.Notification, 0, DirconResponseCode.Success, payload), ct);
    }

    // Always log keypad frames (0x23) and everything on 0004 (handshake); other periodic notifications only every 10th.
    private bool ShouldLogNotification(BleUuid characteristic, byte[] data)
    {
        if (characteristic == DirconUuids.ZwiftSyncTx)
            return true;
        if (characteristic == DirconUuids.ZwiftAsync && data.Length > 0 && (data[0] == 0x23 || data[0] == 0x37 || data[0] == 0x07 || data[0] == 0x42))
            return true;
        lock (_notifyCounters)
        {
            _notifyCounters.TryGetValue(characteristic, out int n);
            _notifyCounters[characteristic] = n + 1;
            return n % 10 == 0;
        }
    }

    private static string MsgName(byte id) => id switch
    {
        DirconMessageId.DiscoverServices => "DiscoverServices(0x01)",
        DirconMessageId.DiscoverCharacteristics => "DiscoverCharacteristics(0x02)",
        DirconMessageId.ReadCharacteristic => "Read(0x03)",
        DirconMessageId.WriteCharacteristic => "Write(0x04)",
        DirconMessageId.EnableNotifications => "EnableNotifications(0x05)",
        DirconMessageId.Notification => "Notification(0x06)",
        _ => $"Unknown(0x{id:X2})",
    };

    private void LogWire(string direction, DirconMessage m)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
            return;
        string uuid = "-";
        string value;
        if (m.MessageId is DirconMessageId.DiscoverServices or DirconMessageId.DiscoverCharacteristics || m.Payload.Length < 16)
        {
            value = Convert.ToHexString(m.Payload);
            if (m.MessageId == DirconMessageId.DiscoverCharacteristics && m.Payload.Length >= 16)
                uuid = BleUuid.FromBytes(m.Payload.AsSpan(0, 16)).ToString();
        }
        else
        {
            uuid = BleUuid.FromBytes(m.Payload.AsSpan(0, 16)).ToString();
            value = Convert.ToHexString(m.Payload.AsSpan(16));
        }
        _logger.LogDebug("DIRCON {Name} {Dir} {MsgId} seq={Seq} resp={Resp} uuid={Uuid} value={Value}",
            _name, direction, MsgName(m.MessageId), m.Seq, m.ResponseCode, uuid, value);
    }

    private async Task SendAsync(DirconMessage message, CancellationToken ct)
    {
        if (message.MessageId != DirconMessageId.Notification)
            LogWire("TX", message);
        var bytes = message.Encode();
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(bytes, ct).ConfigureAwait(false);
            await _stream.FlushAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private Task ReplyAsync(DirconMessage request, byte code, byte[]? payload, CancellationToken ct)
        => SendAsync(DirconMessage.Create(request.MessageId, request.Seq, code, payload), ct);

    private async Task HandleMessageAsync(DirconMessage msg, CancellationToken ct)
    {
        LogWire("RX", msg);
        // Frames with a non-zero response code come from a peer that is answering; ignore them.
        if (msg.ResponseCode != 0)
            return;

        switch (msg.MessageId)
        {
            case DirconMessageId.DiscoverServices:
                {
                    var payload = new byte[16 * _services.Count];
                    for (int i = 0; i < _services.Count; i++)
                        _services[i].Uuid.WriteTo(payload.AsSpan(i * 16, 16));
                    await ReplyAsync(msg, DirconResponseCode.Success, payload, ct).ConfigureAwait(false);
                    break;
                }
            case DirconMessageId.DiscoverCharacteristics:
                {
                    if (msg.Payload.Length != 16)
                    {
                        await ReplyAsync(msg, DirconResponseCode.UnexpectedError, null, ct).ConfigureAwait(false);
                        break;
                    }
                    var svcUuid = BleUuid.FromBytes(msg.Payload);
                    var svc = _services.FirstOrDefault(s => s.Uuid == svcUuid);
                    if (svc is null)
                    {
                        await ReplyAsync(msg, DirconResponseCode.ServiceNotFound, null, ct).ConfigureAwait(false);
                        break;
                    }
                    var payload = new byte[16 + 17 * svc.Characteristics.Count];
                    svc.Uuid.WriteTo(payload.AsSpan(0, 16));
                    for (int i = 0; i < svc.Characteristics.Count; i++)
                    {
                        int o = 16 + 17 * i;
                        svc.Characteristics[i].Uuid.WriteTo(payload.AsSpan(o, 16));
                        payload[o + 16] = (byte)svc.Characteristics[i].Properties;
                    }
                    await ReplyAsync(msg, DirconResponseCode.Success, payload, ct).ConfigureAwait(false);
                    break;
                }
            case DirconMessageId.ReadCharacteristic:
                {
                    if (!TryGetCharacteristic(msg, out var ch, out var uuid))
                    {
                        await ReplyAsync(msg, ch is null && msg.Payload.Length < 16 ? DirconResponseCode.UnexpectedError : DirconResponseCode.CharacteristicNotFound, null, ct).ConfigureAwait(false);
                        break;
                    }
                    if ((ch!.Properties & GattProperties.Read) == 0)
                    {
                        await ReplyAsync(msg, DirconResponseCode.OperationNotSupported, null, ct).ConfigureAwait(false);
                        break;
                    }
                    var value = ch.Value;
                    var payload = new byte[16 + value.Length];
                    uuid.WriteTo(payload.AsSpan(0, 16));
                    value.CopyTo(payload, 16);
                    await ReplyAsync(msg, DirconResponseCode.Success, payload, ct).ConfigureAwait(false);
                    break;
                }
            case DirconMessageId.WriteCharacteristic:
                {
                    if (!TryGetCharacteristic(msg, out var ch, out var uuid))
                    {
                        await ReplyAsync(msg, ch is null && msg.Payload.Length < 16 ? DirconResponseCode.UnexpectedError : DirconResponseCode.CharacteristicNotFound, null, ct).ConfigureAwait(false);
                        break;
                    }
                    if ((ch!.Properties & GattProperties.Write) == 0)
                    {
                        await ReplyAsync(msg, DirconResponseCode.OperationNotSupported, null, ct).ConfigureAwait(false);
                        break;
                    }
                    // Ack: characteristic UUID only, same seq.
                    await ReplyAsync(msg, DirconResponseCode.Success, uuid.ToBytes(), ct).ConfigureAwait(false);

                    var data = msg.Payload.AsSpan(16).ToArray();
                    foreach (var (target, notification) in _protocol.HandleWrite(uuid, data))
                        await NotifyAsync(target, notification, true, ct).ConfigureAwait(false);
                    break;
                }
            case DirconMessageId.EnableNotifications:
                {
                    if (!TryGetCharacteristic(msg, out var ch, out var uuid))
                    {
                        await ReplyAsync(msg, ch is null && msg.Payload.Length < 16 ? DirconResponseCode.UnexpectedError : DirconResponseCode.CharacteristicNotFound, null, ct).ConfigureAwait(false);
                        break;
                    }
                    if ((ch!.Properties & (GattProperties.Notify | GattProperties.Indicate)) == 0)
                    {
                        await ReplyAsync(msg, DirconResponseCode.OperationNotSupported, null, ct).ConfigureAwait(false);
                        break;
                    }
                    bool enable = msg.Payload.Length < 17 || msg.Payload[16] != 0;
                    byte[][] flush = Array.Empty<byte[]>();
                    lock (_gate)
                    {
                        if (enable)
                        {
                            _subscribed.Add(uuid);
                            if (_pending.Remove(uuid, out var q))
                                flush = q.ToArray();
                        }
                        else
                        {
                            _subscribed.Remove(uuid);
                        }
                    }
                    await ReplyAsync(msg, DirconResponseCode.Success, uuid.ToBytes(), ct).ConfigureAwait(false);

                    if (enable)
                    {
                        foreach (var data in flush)
                            await SendNotificationFrameAsync(uuid, data, ct).ConfigureAwait(false);
                        foreach (var (target, notification) in _protocol.OnSubscribed(uuid))
                            await NotifyAsync(target, notification, true, ct).ConfigureAwait(false);
                    }
                    break;
                }
            default:
                _logger.LogDebug("DIRCON {Name}: unknown message id {Id}", _name, msg.MessageId);
                await ReplyAsync(msg, DirconResponseCode.UnexpectedError, null, ct).ConfigureAwait(false);
                break;
        }
    }

    private bool TryGetCharacteristic(DirconMessage msg, out GattCharacteristic? characteristic, out BleUuid uuid)
    {
        characteristic = null;
        uuid = default;
        if (msg.Payload.Length < 16)
            return false;
        uuid = BleUuid.FromBytes(msg.Payload.AsSpan(0, 16));
        foreach (var svc in _services)
            foreach (var ch in svc.Characteristics)
                if (ch.Uuid == uuid)
                {
                    characteristic = ch;
                    return true;
                }
        return false;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        try { _cts.Cancel(); } catch (ObjectDisposedException) { }
        try { await _stream.DisposeAsync().ConfigureAwait(false); } catch (Exception ex) { _logger.LogDebug(ex, "DIRCON session {Name} stream dispose failed", _name); }
    }
}
