using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Network.Dircon;

/// <summary>Link to Zwift that presents the app as a Wahoo KICKR BIKE SHIFT controller over DIRCON.</summary>
public interface IZwiftControllerLink : IAsyncDisposable
{
    /// <summary>TCP port in use (valid after <see cref="StartAsync"/>).</summary>
    int Port { get; }

    /// <summary>True after the RideOn handshake completed.</summary>
    bool IsZwiftConnected { get; }

    /// <summary>Raised with the new value of <see cref="IsZwiftConnected"/>.</summary>
    event EventHandler<bool>? ConnectionChanged;

    /// <summary>Binds the listener (dual-mode IPv6, falls back through port+10) and starts accepting.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Stops listening and closes the current session.</summary>
    Task StopAsync();

    /// <summary>Sends the 0x23 keypad frame on 00000002 to the current session.</summary>
    Task SendButtonsAsync(ZwiftRideButton pressed, CancellationToken ct = default);

    /// <summary>
    /// Sends buttons plus the two analog paddles (ZL = location 0, ZR = location 1, each -100..100). With the BikeControlRide identity
    /// the Ride keypad frame with both analog entries is sent, otherwise the digital-only 0x23 frame. The state is remembered and resent by the keepalive.
    /// </summary>
    Task SendInputAsync(ZwiftRideButton pressed, sbyte zl, sbyte zr, CancellationToken ct = default);

    /// <summary>Battery percentage (0..100, null = default 100) reported in the id-771 battery reply of the BikeControlRide identity.</summary>
    void SetBatteryPercent(int? percent);
}

/// <summary>DIRCON TCP server emulating a KICKR BIKE SHIFT.</summary>
public sealed class DirconServer : IZwiftControllerLink
{
    private static readonly BleUuid SimHeartRate = BleUuid.FromShort(0x2A37);
    private static readonly TimeSpan KeypadKeepAliveInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RideTick = TimeSpan.FromMilliseconds(250);

    private readonly DeviceIdentity _identity;
    private readonly ILogger<DirconServer> _logger;
    private readonly TimeSpan _notificationInterval;
    private readonly DeviceProfileOptions _profile;
    private readonly object _lock = new();

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private Task? _periodicTask;
    private DirconSession? _session;
    private Task? _sessionTask;
    private bool _connected;
    private ZwiftRideButton _lastButtons = ZwiftRideButton.None;
    private sbyte _lastZl;
    private sbyte _lastZr;
    private int? _batteryPercent;
    private int _port;
    private bool _rideActive;
    private int _ridePower;
    private int _rideCadence;
    private readonly RideSimulator _rideSimulator = new();

    /// <summary>Creates the server. No sockets are opened until <see cref="StartAsync"/>.</summary>
    /// <param name="identity">Device identity.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="preferredPort">First port to try (default 36866); up to +10 are tried if busy.</param>
    /// <param name="notificationInterval">Period of idle ride-data / power notifications (default 1 s).</param>
    /// <param name="profile">Advertisement / GATT options presented to Zwift (default: kickr/full).</param>
    public DirconServer(DeviceIdentity identity, ILogger<DirconServer> logger, int preferredPort = 36866, TimeSpan? notificationInterval = null, DeviceProfileOptions? profile = null)
    {
        _profile = profile ?? DeviceProfileOptions.Default;
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _port = preferredPort;
        _notificationInterval = notificationInterval ?? TimeSpan.FromSeconds(1);
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
                    _logger.LogWarning("DIRCON port {Port} unavailable: {Message}", p, ex.Message);
                }
            }
            if (listener is null)
                throw new IOException($"Could not bind a DIRCON port in {first}..{first + 10}.", last);

            _listener = listener;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _acceptTask = Task.Run(() => AcceptLoopAsync(listener, token));
            _periodicTask = Task.Run(() => PeriodicLoopAsync(token));
            _logger.LogInformation("DIRCON server listening on port {Port}", _port);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        TcpListener? listener;
        Task? accept, periodic, sessionTask;
        DirconSession? session;
        lock (_lock)
        {
            cts = _cts; listener = _listener; accept = _acceptTask; periodic = _periodicTask;
            session = _session; sessionTask = _sessionTask;
            _cts = null; _listener = null; _acceptTask = null; _periodicTask = null;
            _session = null; _sessionTask = null;
        }
        if (cts is null)
            return;

        cts.Cancel();
        try { listener?.Stop(); } catch (SocketException) { }
        if (session is not null)
            await session.DisposeAsync().ConfigureAwait(false);
        foreach (var t in new[] { accept, periodic, sessionTask })
        {
            if (t is null) continue;
            try { await t.ConfigureAwait(false); } catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException or IOException) { }
        }
        cts.Dispose();
        SetConnected(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Sent once on the 00000002 UUID. DIRCON addresses characteristics by UUID only, so the FC82 and 00000001-19CA instances of
    /// 00000002 are one notification target on the wire; the BikeControl capture shows its own idle keypad frame once per 5 s as well.
    /// </remarks>
    public Task SendButtonsAsync(ZwiftRideButton pressed, CancellationToken ct = default) => SendInputAsync(pressed, 0, 0, ct);

    /// <inheritdoc />
    public async Task SendInputAsync(ZwiftRideButton pressed, sbyte zl, sbyte zr, CancellationToken ct = default)
    {
        DirconSession? session;
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
            await session.NotifyAsync(DirconUuids.ZwiftAsync, frame, false, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            _logger.LogDebug(ex, "Failed to send keypad frame");
        }
    }

    /// <inheritdoc />
    public void SetBatteryPercent(int? percent)
    {
        lock (_lock)
            _batteryPercent = percent;
    }

    private byte[] BuildInputFrame(ZwiftRideButton pressed, sbyte zl, sbyte zr) =>
        _profile.Controller == ControllerIdentity.BikeControlRide
            ? ZwiftControlProtocol.BuildRideKeyPadFrame(pressed, new RideAnalogEntry(0, zl), new RideAnalogEntry(1, zr))
            : ZwiftControlProtocol.BuildKeyPadFrame(pressed);

    /// <summary>True while a simulated ride is active.</summary>
    public bool IsRideSimulated
    {
        get { lock (_lock) return _rideActive; }
    }

    /// <summary>
    /// Starts (or updates) a simulated ride: 2A63, 2AD2 and the 0x03 frame on 00000002 carry these values at 4 Hz.
    /// Without this call the server keeps its idle behaviour (all zeros, 1 Hz).
    /// </summary>
    public void SetSimulatedRide(int powerWatts, int cadenceRpm)
    {
        lock (_lock)
        {
            _rideActive = true;
            _ridePower = Math.Max(0, powerWatts);
            _rideCadence = Math.Max(0, cadenceRpm);
        }
        _logger.LogInformation("Simulated ride ON: {Power} W, {Cadence} rpm", Math.Max(0, powerWatts), Math.Max(0, cadenceRpm));
    }

    /// <summary>Stops the simulated ride and returns to idle (zero) notifications.</summary>
    public void StopSimulatedRide()
    {
        lock (_lock)
        {
            _rideActive = false;
            _ridePower = 0;
            _rideCadence = 0;
        }
        _logger.LogInformation("Simulated ride OFF");
    }

    /// <summary>
    /// Reports the virtual gear like the real bike: 0x42 frame with the front chainring ({1: front}, 42 32 02 08 FF), about 12 ms later
    /// the rear cog ({4: rear}, 42 32 02 20 NN). Zwift's HUD showed "1-1".."1-5" for these values. Only when gearing is enabled.
    /// </summary>
    public async Task SendGearAsync(int rearCog, int frontChainring = 1, CancellationToken ct = default)
    {
        if (!_profile.EnableWahooGearing || _profile.IsBikeControlReplica)
        {
            _logger.LogInformation("0x42 gear frames are disabled (gearing off)");
            return;
        }
        DirconSession? session;
        lock (_lock)
            session = _session;
        if (session is null)
            return;
        try
        {
            await session.NotifyAsync(DirconUuids.ZwiftAsync, ZwiftControlProtocol.BuildChainringFrame(frontChainring), false, ct).ConfigureAwait(false);
            await Task.Delay(12, ct).ConfigureAwait(false);
            await session.NotifyAsync(DirconUuids.ZwiftAsync, ZwiftControlProtocol.BuildRearCogFrame(rearCog), false, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            _logger.LogDebug(ex, "Failed to send gear frame");
        }
    }

    /// <summary>Sends a raw frame on 00000002 (AsyncOut) to the current session, e.g. a Click (0x37) or Play (0x07) keypad frame.</summary>
    public async Task SendAsyncFrameAsync(byte[] frame, CancellationToken ct = default)
    {
        DirconSession? session;
        lock (_lock)
            session = _session;
        if (session is null)
            return;
        try
        {
            await session.NotifyAsync(DirconUuids.ZwiftAsync, frame, false, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            _logger.LogDebug(ex, "Failed to send frame");
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
                _logger.LogWarning(ex, "DIRCON accept failed");
                continue;
            }

            try { client.NoDelay = true; } catch (SocketException) { }
            string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            _logger.LogInformation("DIRCON client connected from {Remote}", remote);

            var protocol = new ZwiftControlProtocol(_identity, _logger, _profile.Controller, () => { lock (_lock) return _batteryPercent; });
            var session = new DirconSession(client.GetStream(), DeviceProfiles.CreateServices(_profile, _identity), protocol, _logger, remote);
            session.HandshakeCompleted += (_, _) => OnHandshake(session);

            DirconSession? old;
            lock (_lock)
            {
                old = _session;
                _session = session;
            }
            if (old is not null)
            {
                _logger.LogInformation("Replacing previous DIRCON client");
                await old.DisposeAsync().ConfigureAwait(false);
            }

            var runTask = RunSessionAsync(session, client, remote, ct);
            lock (_lock)
                _sessionTask = runTask;
        }
    }

    private async Task RunSessionAsync(DirconSession session, TcpClient client, string remote, CancellationToken ct)
    {
        try
        {
            await session.RunAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DIRCON session {Remote} ended with error", remote);
        }
        finally
        {
            await session.DisposeAsync().ConfigureAwait(false);
            try { client.Dispose(); } catch (Exception ex) { _logger.LogDebug(ex, "Client dispose failed"); }
            bool wasCurrent;
            lock (_lock)
            {
                wasCurrent = ReferenceEquals(_session, session);
                if (wasCurrent)
                    _session = null;
            }
            _logger.LogInformation("DIRCON client {Remote} disconnected", remote);
            if (wasCurrent)
                SetConnected(false);
        }
    }

    private void OnHandshake(DirconSession session)
    {
        bool current;
        lock (_lock)
            current = ReferenceEquals(_session, session);
        _logger.LogInformation("DIRCON RideOn handshake completed (current session: {Current})", current);
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
        catch (Exception ex) { _logger.LogError(ex, "ConnectionChanged handler failed"); }
    }

    private async Task PeriodicLoopAsync(CancellationToken ct)
    {
        var idleInterval = _notificationInterval > TimeSpan.Zero ? _notificationInterval : TimeSpan.FromSeconds(1);
        // Simulated rides tick at 4 Hz like the real bike (250 ms); idle notifications keep the configured interval.
        var tick = idleInterval < RideTick ? idleInterval : RideTick;
        int idleEvery = Math.Max(1, (int)Math.Round(idleInterval.TotalMilliseconds / tick.TotalMilliseconds));
        int e03cEvery = Math.Max(1, (int)Math.Round(1000.0 / tick.TotalMilliseconds));
        using var timer = new PeriodicTimer(tick);
        var lastKeypad = DateTime.UtcNow;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        double lastElapsed = 0;
        long tickCount = 0;
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                tickCount++;
                double elapsed = watch.Elapsed.TotalSeconds;
                double dt = elapsed - lastElapsed;
                lastElapsed = elapsed;

                DirconSession? session;
                ZwiftRideButton buttons;
                sbyte lastZl, lastZr;
                bool ride;
                int power, cadence;
                lock (_lock)
                {
                    session = _session;
                    buttons = _lastButtons;
                    lastZl = _lastZl;
                    lastZr = _lastZr;
                    ride = _rideActive;
                    power = _ridePower;
                    cadence = _rideCadence;
                }
                if (session is null)
                    continue;

                try
                {
                    // The BikeControl replica sends no 0x03/0x42/E03C, no HR/2A63 and (unless a ride is simulated) no FTMS notifications, only the keypad frame below.
                    bool replica = _profile.IsBikeControlReplica;
                    if (ride && replica)
                    {
                        // Simulated ride in the BikeControl replica: only FTMS Indoor Bike Data (2AD2, Notify in the replica GATT) at 4 Hz,
                        // so Zwift shows power/cadence when this device is picked as Power Source/Cadence. No 2A63 (the replica has no 1818 service).
                        var sample = _rideSimulator.Step(power, cadence, dt);
                        await session.NotifyAsync(DirconUuids.IndoorBikeData,
                            ZwiftControlProtocol.BuildIndoorBikeData(sample.SpeedCentiKmh, sample.CadenceRpm, sample.PowerWatts), false, ct).ConfigureAwait(false);
                        if (_profile.AddPowerService)
                            await session.NotifyAsync(DirconUuids.CyclingPowerMeasurement,
                                ZwiftControlProtocol.BuildPowerMeasurement(sample.PowerWatts, sample.AccumulatedTorque, sample.WheelRevolutions, sample.WheelEventTime, sample.CrankRevolutions, sample.CrankEventTime), false, ct).ConfigureAwait(false);
                        if (tickCount % e03cEvery == 0) // 1 Hz Heart Rate Measurement (flags 0x00, 120 bpm); Zwift subscribes to 2A37
                            await session.NotifyAsync(SimHeartRate, new byte[] { 0x00, 120 }, false, ct).ConfigureAwait(false);
                    }
                    else if (ride && !replica)
                    {
                        var sample = _rideSimulator.Step(power, cadence, dt);
                        if (_profile.EnableWahooGearing)
                            await session.NotifyAsync(DirconUuids.ZwiftAsync,
                                ZwiftControlProtocol.BuildRideData(sample.PowerWatts, sample.CadenceRpm, sample.SpeedCentimetresPerSecond, sample.WheelRpmTimes100), false, ct).ConfigureAwait(false);
                        await session.NotifyAsync(DirconUuids.CyclingPowerMeasurement,
                            ZwiftControlProtocol.BuildPowerMeasurement(sample.PowerWatts, sample.AccumulatedTorque, sample.WheelRevolutions, sample.WheelEventTime, sample.CrankRevolutions, sample.CrankEventTime), false, ct).ConfigureAwait(false);
                        await session.NotifyAsync(DirconUuids.IndoorBikeData,
                            ZwiftControlProtocol.BuildIndoorBikeData(sample.SpeedCentiKmh, sample.CadenceRpm, sample.PowerWatts), false, ct).ConfigureAwait(false);
                        if (_profile.HasWahooServices && tickCount % e03cEvery == 0)
                            await session.NotifyAsync(DirconUuids.WahooE03C, KickrBikeProfile.IdleE03C, false, ct).ConfigureAwait(false);
                    }
                    else if (!replica && tickCount % idleEvery == 0)
                    {
                        if (_profile.EnableWahooGearing)
                            await session.NotifyAsync(DirconUuids.ZwiftAsync, ZwiftControlProtocol.BuildRideData(0, 0), false, ct).ConfigureAwait(false);
                        await session.NotifyAsync(DirconUuids.CyclingPowerMeasurement, KickrBikeProfile.IdlePowerMeasurement, false, ct).ConfigureAwait(false);
                        await session.NotifyAsync(DirconUuids.IndoorBikeData, KickrBikeProfile.IdleIndoorBikeData, false, ct).ConfigureAwait(false);
                        if (_profile.HasWahooServices)
                            await session.NotifyAsync(DirconUuids.WahooE03C, KickrBikeProfile.IdleE03C, false, ct).ConfigureAwait(false);
                    }

                    var now = DateTime.UtcNow;
                    if (now - lastKeypad >= KeypadKeepAliveInterval)
                    {
                        lastKeypad = now;
                        await session.NotifyAsync(DirconUuids.ZwiftAsync, BuildInputFrame(buttons, lastZl, lastZr), false, ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
                {
                    _logger.LogDebug(ex, "Periodic notification failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
