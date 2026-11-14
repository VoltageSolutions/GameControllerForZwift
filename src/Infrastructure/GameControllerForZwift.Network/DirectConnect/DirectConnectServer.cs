using System.Net;
using System.Net.Sockets;
using GameControllerForZwift.Network.ZwiftRide;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Network.DirectConnect;

/// <summary>Wahoo Direct Connect (DIRCON): Bluetooth LE GATT tunnelled over TCP. Server presenting the Zwift Ride profile to Zwift.</summary>
public sealed partial class DirectConnectServer : IZwiftRideLink
{
    private static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(5);

    private readonly ILogger<DirectConnectServer> _logger;
    private readonly TimeSpan _keepAliveInterval;
    private readonly object _lock = new();

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private Task? _keepAliveTask;
    private DirectConnectSession? _session;
    private Task? _sessionTask;
    private bool _connected;
    private ZwiftRideButton _lastButtons = ZwiftRideButton.None;
    private sbyte _lastZl;
    private sbyte _lastZr;
    private int? _batteryPercent;
    private int _port;

    /// <summary>Creates the server. No sockets are opened until <see cref="StartAsync"/>.</summary>
    /// <param name="logger">Logger.</param>
    /// <param name="preferredPort">First port to try (default 36866); up to +10 are tried if busy.</param>
    /// <param name="keepAliveInterval">Period at which the current keypad frame is resent (default 5 s, as the real device does).</param>
    public DirectConnectServer(ILogger<DirectConnectServer> logger, int preferredPort = 36866, TimeSpan? keepAliveInterval = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _port = preferredPort;
        _keepAliveInterval = keepAliveInterval is { } k && k > TimeSpan.Zero ? k : DefaultKeepAliveInterval;
    }

    /// <inheritdoc />
    public int Port => _port;

    /// <inheritdoc />
    public bool IsZwiftConnected
    {
        get { lock (_lock) return _connected; }
    }

    /// <inheritdoc />
    public event EventHandler<bool>? ConnectionChanged;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_listener is not null)
                return Task.CompletedTask;

            int first = _port;
            TcpListener? listener = null;
            SocketException? last = null;
            for (int p = first; p <= first + 10; p++)
            {
                var candidate = Socket.OSSupportsIPv6
                    ? new TcpListener(IPAddress.IPv6Any, p)
                    : new TcpListener(IPAddress.Any, p);
                try
                {
                    if (Socket.OSSupportsIPv6)
                        candidate.Server.DualMode = true;
                    candidate.Start();
                    listener = candidate;
                    _port = p;
                    break;
                }
                catch (SocketException ex)
                {
                    last = ex;
                    candidate.Stop();
                    LogPortUnavailable(p, ex.Message);
                }
            }
            if (listener is null)
                throw new IOException($"Could not bind a DIRCON port in {first}..{first + 10}.", last);

            _listener = listener;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _acceptTask = Task.Run(() => AcceptLoopAsync(listener, token), token);
            _keepAliveTask = Task.Run(() => KeepAliveLoopAsync(token), token);
            LogListening(_port);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        TcpListener? listener;
        Task? accept, keepAlive, sessionTask;
        DirectConnectSession? session;
        lock (_lock)
        {
            cts = _cts; listener = _listener; accept = _acceptTask; keepAlive = _keepAliveTask;
            session = _session; sessionTask = _sessionTask;
            _cts = null; _listener = null; _acceptTask = null; _keepAliveTask = null;
            _session = null; _sessionTask = null;
        }
        if (cts is null)
            return;

        cts.Cancel();
        try { listener?.Stop(); } catch (SocketException) { }
        if (session is not null)
            await session.DisposeAsync().ConfigureAwait(false);
        foreach (var t in new[] { accept, keepAlive, sessionTask })
        {
            if (t is null) continue;
            try { await t.ConfigureAwait(false); } catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException or IOException) { }
        }
        cts.Dispose();
        SetConnected(false);
    }

    /// <inheritdoc />
    public async Task SendInputAsync(ZwiftRideButton pressed, sbyte zl, sbyte zr, CancellationToken ct = default)
    {
        DirectConnectSession? session;
        lock (_lock)
        {
            _lastButtons = pressed;
            _lastZl = zl;
            _lastZr = zr;
            session = _session;
        }
        if (session is null)
            return;
        var frame = BuildInputFrame(pressed, zl, zr);
        try
        {
            await session.NotifyAsync(ZwiftRideUuids.AsyncOut, frame, false, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            LogSendKeypadFailed(ex);
        }
    }

    /// <inheritdoc />
    public void SetBatteryPercent(int? percent)
    {
        lock (_lock)
            _batteryPercent = percent;
    }

    private static byte[] BuildInputFrame(ZwiftRideButton pressed, sbyte zl, sbyte zr) =>
        ZwiftControlProtocol.BuildRideKeyPadFrame(pressed, new RideAnalogEntry(0, zl), new RideAnalogEntry(1, zr));

    /// <summary>
    /// Sends a raw frame on AsyncOut to the current session. Diagnostics only (the probe's analog lab); normal output goes through
    /// <see cref="SendInputAsync"/>.
    /// </summary>
    public async Task SendKeypadFrameAsync(byte[] frame, CancellationToken ct = default)
    {
        DirectConnectSession? session;
        lock (_lock)
            session = _session;
        if (session is null)
            return;
        try
        {
            await session.NotifyAsync(ZwiftRideUuids.AsyncOut, frame, false, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            LogSendFrameFailed(ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException ex)
            {
                if (ct.IsCancellationRequested) break;
                LogAcceptFailed(ex);
                continue;
            }

            try { client.NoDelay = true; } catch (SocketException) { }
            string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            LogClientConnected(remote);

            var protocol = new ZwiftControlProtocol(_logger, () => { lock (_lock) return _batteryPercent; });
            var session = new DirectConnectSession(client.GetStream(), ZwiftRideProfile.CreateServices(), protocol, _logger, remote);
            session.HandshakeCompleted += (_, _) => OnHandshake(session);

            DirectConnectSession? old;
            lock (_lock)
            {
                old = _session;
                _session = session;
            }
            if (old is not null)
            {
                LogReplacingClient();
                await old.DisposeAsync().ConfigureAwait(false);
            }

            var runTask = RunSessionAsync(session, client, remote, ct);
            lock (_lock)
                _sessionTask = runTask;
        }
    }

    private async Task RunSessionAsync(DirectConnectSession session, TcpClient client, string remote, CancellationToken ct)
    {
        try
        {
            await session.RunAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogSessionError(ex, remote);
        }
        finally
        {
            await session.DisposeAsync().ConfigureAwait(false);
            try { client.Dispose(); } catch (Exception ex) { LogClientDisposeFailed(ex); }
            bool wasCurrent;
            lock (_lock)
            {
                wasCurrent = ReferenceEquals(_session, session);
                if (wasCurrent)
                    _session = null;
            }
            LogClientDisconnected(remote);
            if (wasCurrent)
                SetConnected(false);
        }
    }

    private void OnHandshake(DirectConnectSession session)
    {
        bool current;
        lock (_lock)
            current = ReferenceEquals(_session, session);
        LogHandshakeCompleted(current);
        if (current)
            SetConnected(true);
    }

    private void SetConnected(bool value)
    {
        lock (_lock)
        {
            if (_connected == value)
                return;
            _connected = value;
        }
        try { ConnectionChanged?.Invoke(this, value); }
        catch (Exception ex) { LogConnectionHandlerFailed(ex); }
    }

    /// <summary>
    /// Resends the current keypad frame every keep-alive interval, like the real device's idle frame (23 08 ff ff ff ff 0f every 5 s).
    /// Nothing else is sent unsolicited: the Zwift Ride profile has no ride data, FTMS or heart rate notifications.
    /// </summary>
    private async Task KeepAliveLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_keepAliveInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                DirectConnectSession? session;
                ZwiftRideButton buttons;
                sbyte zl, zr;
                lock (_lock)
                {
                    session = _session;
                    buttons = _lastButtons;
                    zl = _lastZl;
                    zr = _lastZr;
                }
                if (session is null)
                    continue;

                try
                {
                    await session.NotifyAsync(ZwiftRideUuids.AsyncOut, BuildInputFrame(buttons, zl, zr), false, ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
                {
                    LogKeepAliveFailed(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    #region Logging

    [LoggerMessage(Level = LogLevel.Warning, Message = "DIRCON port {Port} unavailable: {Message}")]
    private partial void LogPortUnavailable(int port, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "DIRCON server listening on port {Port}")]
    private partial void LogListening(int port);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to send keypad frame")]
    private partial void LogSendKeypadFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to send frame")]
    private partial void LogSendFrameFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DIRCON accept failed")]
    private partial void LogAcceptFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "DIRCON client connected from {Remote}")]
    private partial void LogClientConnected(string remote);

    [LoggerMessage(Level = LogLevel.Information, Message = "Replacing previous DIRCON client")]
    private partial void LogReplacingClient();

    [LoggerMessage(Level = LogLevel.Debug, Message = "DIRCON session {Remote} ended with error")]
    private partial void LogSessionError(Exception exception, string remote);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client dispose failed")]
    private partial void LogClientDisposeFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "DIRCON client {Remote} disconnected")]
    private partial void LogClientDisconnected(string remote);

    [LoggerMessage(Level = LogLevel.Information, Message = "DIRCON RideOn handshake completed (current session: {Current})")]
    private partial void LogHandshakeCompleted(bool current);

    [LoggerMessage(Level = LogLevel.Error, Message = "ConnectionChanged handler failed")]
    private partial void LogConnectionHandlerFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Keep-alive notification failed")]
    private partial void LogKeepAliveFailed(Exception exception);

    #endregion
}
