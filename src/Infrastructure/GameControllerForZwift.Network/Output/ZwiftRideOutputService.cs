using GameControllerForZwift.Core;
using GameControllerForZwift.Network.Mdns;
using GameControllerForZwift.Network.ZwiftRide;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Network.Output
{
    /// <summary>
    /// <see cref="IOutputService"/> that delivers the controller state to Zwift as an emulated Zwift Ride controller: it owns the
    /// Direct Connect (DIRCON) server and its mDNS advertisement, the 100 ms send loop, the button map, the analog paddle math and the battery report.
    /// Lifecycle: call <see cref="StartAsync"/> at app startup (starts the Direct Connect server, then advertises it via mDNS)
    /// and <see cref="StopAsync"/>/<see cref="DisposeAsync"/> on exit. There are no constructor side effects.
    /// </summary>
    public sealed partial class ZwiftRideOutputService : IOutputService, IAsyncDisposable
    {
        /// <summary>
        /// Single place to edit the ZwiftFunction -> Zwift Ride button mapping (exactly what a Zwift Ride provides).
        /// Shift up = right shifter (SHFT_UP_R), shift down = left shifter (SHFT_UP_L); D-pad = Up/Down/Left/Right;
        /// A = select, B = back/menu, Y = power-up, Z = Ride On. Steering and braking are NOT buttons: they are the
        /// analog paddles (see <see cref="PaddleMath"/>).
        /// </summary>
        public static readonly IReadOnlyDictionary<ZwiftFunction, ZwiftRideButton> ButtonMap = new Dictionary<ZwiftFunction, ZwiftRideButton>
        {
            [ZwiftFunction.NavigateUp] = ZwiftRideButton.Up,
            [ZwiftFunction.NavigateDown] = ZwiftRideButton.Down,
            [ZwiftFunction.NavigateLeft] = ZwiftRideButton.Left,
            [ZwiftFunction.NavigateRight] = ZwiftRideButton.Right,
            [ZwiftFunction.Select] = ZwiftRideButton.A,
            [ZwiftFunction.GoBack] = ZwiftRideButton.B,
            [ZwiftFunction.Powerup] = ZwiftRideButton.Y,
            [ZwiftFunction.RideOn] = ZwiftRideButton.Z,
            [ZwiftFunction.ShiftUp] = ZwiftRideButton.RS1,
            [ZwiftFunction.ShiftDown] = ZwiftRideButton.LS1,
        };

        private readonly IZwiftRideLink _link;
        private readonly MdnsAdvertiser _mdns;
        private readonly DeviceIdentity _identity;
        private readonly ILogger<ZwiftRideOutputService> _logger;

        private static readonly TimeSpan DefaultResendInterval = TimeSpan.FromMilliseconds(100);

        private readonly TimeSpan _resendInterval;
        private readonly object _stateLock = new();
        private readonly SemaphoreSlim _signal = new(0, int.MaxValue);
        private OutputState _state = OutputState.Neutral;
        private int? _lastBattery;
        private CancellationTokenSource? _loopCts;
        private Task? _loopTask;

        private readonly object _startGate = new();
        private bool _started;

        public ZwiftRideOutputService(IZwiftRideLink link, MdnsAdvertiser mdns, DeviceIdentity identity, ILogger<ZwiftRideOutputService> logger, TimeSpan? resendInterval = null)
        {
            _resendInterval = resendInterval ?? DefaultResendInterval;
            _link = link;
            _mdns = mdns;
            _identity = identity;
            _logger = logger;
        }

        /// <summary>Raised when Zwift connects to / disconnects from the emulated controller.</summary>
        public event EventHandler<bool>? ConnectionChanged;

        public bool IsZwiftConnected => _link.IsZwiftConnected;

        /// <summary>Non-blocking: stores the latest state and wakes the send loop when the resulting frame content changed.</summary>
        public Task UpdateAsync(OutputState state, CancellationToken ct = default)
        {
            bool changed;
            bool batteryChanged;
            lock (_stateLock)
            {
                changed = ComputeInput(state) != ComputeInput(_state);
                _state = state;
                batteryChanged = state.BatteryPercent != _lastBattery;
                _lastBattery = state.BatteryPercent;
            }
            if (batteryChanged)
                _link.SetBatteryPercent(state.BatteryPercent);
            if (changed)
                SignalLoop();
            return Task.CompletedTask;
        }

        private static (ZwiftRideButton Mask, sbyte Zl, sbyte Zr) ComputeInput(OutputState state)
        {
            var mask = ZwiftRideButton.None;
            foreach (var f in state.Pressed)
            {
                if (ButtonMap.TryGetValue(f, out var b))
                    mask |= b;
            }
            var (zl, zr) = PaddleMath.Compute(state.Steer, state.Brake);
            return (mask, zl, zr);
        }

        private void SignalLoop()
        {
            try { _signal.Release(); } catch (SemaphoreFullException) { /* a wake-up is already pending */ }
        }

        internal void StartSendLoop()
        {
            if (_loopTask is not null) return;
            _loopCts = new CancellationTokenSource();
            var token = _loopCts.Token;
            _loopTask = Task.Run(() => SendLoopAsync(token));
        }

        internal async Task StopSendLoopAsync()
        {
            var cts = _loopCts; var task = _loopTask;
            _loopCts = null; _loopTask = null;
            if (cts is null) return;
            cts.Cancel();
            try { if (task is not null) await task.ConfigureAwait(false); } catch (OperationCanceledException) { /* send loop cancelled on stop */ }
            cts.Dispose();
        }

        private async Task SendLoopAsync(CancellationToken ct)
        {
            var lastSent = (Mask: ZwiftRideButton.None, Zl: (sbyte)0, Zr: (sbyte)0);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(_resendInterval, ct).ConfigureAwait(false);
                    while (_signal.CurrentCount > 0 && _signal.Wait(0, ct)) { } // coalesce queued wake-ups
                }
                catch (OperationCanceledException) { break; }

                if (!_link.IsZwiftConnected)
                {
                    lastSent = (ZwiftRideButton.None, 0, 0);
                    continue;
                }

                OutputState state;
                lock (_stateLock) state = _state;
                var current = ComputeInput(state);
                bool active = current.Mask != ZwiftRideButton.None || current.Zl != 0 || current.Zr != 0;
                if (!active && current == lastSent)
                    continue; // idle: nothing to send
                try
                {
                    await _link.SendInputAsync(current.Mask, current.Zl, current.Zr, ct).ConfigureAwait(false);
                    lastSent = current;
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    LogSendFailed(ex);
                }
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_startGate)
            {
                if (_started) return;
                _started = true;
            }

            _link.ConnectionChanged += OnLinkConnectionChanged;
            try
            {
                await _link.StartAsync(cancellationToken).ConfigureAwait(false);
                _mdns.Start(_identity, _link.Port);
                StartSendLoop();
                LogStarted(_link.Port);
            }
            catch
            {
                _link.ConnectionChanged -= OnLinkConnectionChanged;
                lock (_startGate) _started = false;
                throw;
            }
        }

        public async Task StopAsync()
        {
            lock (_startGate)
            {
                if (!_started) return;
                _started = false;
            }

            _link.ConnectionChanged -= OnLinkConnectionChanged;
            _mdns.Stop();
            await StopSendLoopAsync().ConfigureAwait(false);
            await _link.StopAsync().ConfigureAwait(false);
        }

        private void OnLinkConnectionChanged(object? sender, bool connected)
        {
            if (!connected)
            {
                lock (_stateLock) _state = OutputState.Neutral;
                SignalLoop();
            }
            ConnectionChanged?.Invoke(this, connected);
        }

        public async ValueTask DisposeAsync()
        {
            try { await StopAsync().ConfigureAwait(false); }
            catch (Exception ex) { LogStopFailed(ex); }
            await _link.DisposeAsync().ConfigureAwait(false);
            await _mdns.DisposeAsync().ConfigureAwait(false);
        }

        #region Logging

        [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to send controller input.")]
        private partial void LogSendFailed(Exception exception);

        [LoggerMessage(Level = LogLevel.Information, Message = "Zwift Ride controller started on port {Port}")]
        private partial void LogStarted(int port);

        [LoggerMessage(Level = LogLevel.Debug, Message = "Error stopping Zwift Ride controller.")]
        private partial void LogStopFailed(Exception exception);

        #endregion
    }
}
