using GameControllerForZwift.Core;
using GameControllerForZwift.Network.Dircon;
using GameControllerForZwift.Network.Mdns;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Network
{
    /// <summary>
    /// Output service that presses buttons on an emulated Zwift Ride controller exposed through a Wahoo DirCon (LAN) server.
    /// Lifecycle: call <see cref="StartAsync"/> at app startup (starts the DirCon server, then advertises it via mDNS)
    /// and <see cref="StopAsync"/>/<see cref="DisposeAsync"/> on exit. There are no constructor side effects.
    /// </summary>
    public sealed class NetworkService : ILanOutputService, IAsyncDisposable
    {
        /// <summary>
        /// Single place to edit the ZwiftFunction -> Zwift Ride button mapping.
        /// Per docs/zwift-lan-protocol-notes.md: shift up = right shifter (SHFT_UP_R), shift down = left shifter (SHFT_UP_L);
        /// steering and braking are NOT buttons any more: they are the analog paddles (see <see cref="PaddleMath"/>);
        /// UP = in-ride menu, DOWN = U-turn; A = select, B = back, Y = power-up.
        /// RiderAction maps to Z (Ride On bomb) and is handled specially (only when the chosen rider action is RideOn).
        /// </summary>
        public static readonly IReadOnlyDictionary<ZwiftFunction, ZwiftRideButton> ButtonMap = new Dictionary<ZwiftFunction, ZwiftRideButton>
        {
            [ZwiftFunction.ShiftUp] = ZwiftRideButton.RS1,
            [ZwiftFunction.ShiftDown] = ZwiftRideButton.LS1,
            [ZwiftFunction.NavigateLeft] = ZwiftRideButton.Left,
            [ZwiftFunction.NavigateRight] = ZwiftRideButton.Right,
            [ZwiftFunction.NavigateUp] = ZwiftRideButton.Up,
            [ZwiftFunction.NavigateDown] = ZwiftRideButton.Down,
            [ZwiftFunction.ShowMenu] = ZwiftRideButton.Up,
            [ZwiftFunction.Uturn] = ZwiftRideButton.Down,
            [ZwiftFunction.Select] = ZwiftRideButton.A,
            [ZwiftFunction.GoBack] = ZwiftRideButton.B,
            [ZwiftFunction.Powerup] = ZwiftRideButton.Y,
        };

        /// <summary>
        /// How long a button stays pressed after the last PerformActionAsync call for it. DataIntegrator calls PerformActionAsync
        /// repeatedly (every poll) while the physical button is held, so each call extends the hold; a tap is held for this long.
        /// </summary>
        public static readonly TimeSpan HoldTime = TimeSpan.FromMilliseconds(120);

        private readonly IZwiftControllerLink _link;
        private readonly MdnsAdvertiser _mdns;
        private readonly DeviceIdentity _identity;
        private readonly ILogger<NetworkService> _logger;

        private static readonly TimeSpan DefaultResendInterval = TimeSpan.FromMilliseconds(100);

        private readonly TimeSpan _resendInterval;
        private readonly object _stateLock = new();
        private readonly SemaphoreSlim _signal = new(0, int.MaxValue);
        private ZwiftControllerState _state = ZwiftControllerState.Neutral;
        private int? _lastBattery;
        private CancellationTokenSource? _loopCts;
        private Task? _loopTask;

        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly Dictionary<ZwiftRideButton, CancellationTokenSource> _held = new();
        private readonly object _startGate = new();
        private bool _started;

        public NetworkService(IZwiftControllerLink link, MdnsAdvertiser mdns, DeviceIdentity identity, ILogger<NetworkService> logger, TimeSpan? resendInterval = null)
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

        bool ILanOutputService.IsAvailable => IsZwiftConnected;

        public bool Supports(ZwiftFunction zwiftFunction, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand)
            => CanHandle(zwiftFunction, riderAction);

        public bool CanHandle(ZwiftFunction function, ZwiftRiderAction riderAction)
        {
            if (function == ZwiftFunction.RiderAction)
                return riderAction == ZwiftRiderAction.RideOn;
            return ButtonMap.ContainsKey(function)
                || function == ZwiftFunction.SteerLeft
                || function == ZwiftFunction.SteerRight
                || function == ZwiftFunction.Brake;
        }

        /// <summary>Non-blocking: stores the latest state and wakes the send loop when the resulting frame content changed.</summary>
        public Task UpdateAsync(ZwiftControllerState state, CancellationToken ct = default)
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

        private static (ZwiftRideButton Mask, sbyte Zl, sbyte Zr) ComputeInput(ZwiftControllerState state)
        {
            var mask = ZwiftRideButton.None;
            foreach (var f in state.Pressed)
            {
                if (f == ZwiftFunction.RiderAction)
                    mask |= ZwiftRideButton.Z;
                else if (ButtonMap.TryGetValue(f, out var b))
                    mask |= b;
            }
            var (zl, zr) = PaddleMath.Compute(state.Steer, state.Brake);
            return (mask, zl, zr);
        }

        private void SignalLoop()
        {
            try { _signal.Release(); } catch (SemaphoreFullException) { }
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
            try { if (task is not null) await task.ConfigureAwait(false); } catch (OperationCanceledException) { }
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
                    while (_signal.CurrentCount > 0 && _signal.Wait(0)) { } // coalesce queued wake-ups
                }
                catch (OperationCanceledException) { break; }

                if (!_link.IsZwiftConnected)
                {
                    lastSent = (ZwiftRideButton.None, 0, 0);
                    continue;
                }

                ZwiftControllerState state;
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
                    _logger.LogDebug(ex, "Failed to send controller input.");
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
                _logger.LogInformation("Zwift LAN controller started on port {Port}", _link.Port);
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
            ReleaseAllHeld();
            await _link.StopAsync().ConfigureAwait(false);
        }

        public async Task<ActionResult> PerformActionAsync(ZwiftFunction zwiftFunction, ZwiftPlayerView playerView = ZwiftPlayerView.Default, ZwiftRiderAction riderAction = ZwiftRiderAction.WaveHand)
        {
            ZwiftRideButton button;
            if (zwiftFunction == ZwiftFunction.RiderAction && riderAction == ZwiftRiderAction.RideOn)
                button = ZwiftRideButton.Z;
            else if (!ButtonMap.TryGetValue(zwiftFunction, out button))
                return new ActionResult { Success = false, ErrorMessage = $"No Zwift LAN button mapping for {zwiftFunction}." };

            if (!IsZwiftConnected)
                return new ActionResult { Success = false, ErrorMessage = "Zwift is not connected to the LAN controller." };

            try
            {
                await _sendLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    var cts = new CancellationTokenSource();
                    var alreadyHeld = _held.TryGetValue(button, out var previous);
                    if (alreadyHeld)
                        previous!.Cancel(); // extend the hold; no new press frame needed
                    _held[button] = cts;

                    if (!alreadyHeld)
                        await _link.SendButtonsAsync(CurrentMask()).ConfigureAwait(false);

                    _ = ReleaseAfterDelayAsync(button, cts);
                }
                finally
                {
                    _sendLock.Release();
                }
                return new ActionResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending {Function} to Zwift over the network.", zwiftFunction);
                return new ActionResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private ZwiftRideButton CurrentMask()
        {
            var mask = ZwiftRideButton.None;
            foreach (var b in _held.Keys) mask |= b;
            return mask;
        }

        private async Task ReleaseAfterDelayAsync(ZwiftRideButton button, CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(HoldTime, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return; // superseded by a newer press of the same button
            }

            try
            {
                await _sendLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_held.TryGetValue(button, out var current) && ReferenceEquals(current, cts))
                    {
                        _held.Remove(button);
                        cts.Dispose();
                        await _link.SendButtonsAsync(CurrentMask()).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _sendLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release {Button}.", button);
            }
        }

        private void ReleaseAllHeld()
        {
            _sendLock.Wait();
            try
            {
                foreach (var cts in _held.Values) cts.Cancel();
                _held.Clear();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private void OnLinkConnectionChanged(object? sender, bool connected)
        {
            if (!connected)
            {
                ReleaseAllHeld();
                lock (_stateLock) _state = ZwiftControllerState.Neutral;
                SignalLoop();
            }
            ConnectionChanged?.Invoke(this, connected);
        }

        public async ValueTask DisposeAsync()
        {
            try { await StopAsync().ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogDebug(ex, "Error stopping Zwift LAN controller."); }
            await _link.DisposeAsync().ConfigureAwait(false);
            await _mdns.DisposeAsync().ConfigureAwait(false);
        }
    }
}

namespace GameControllerForZwift.Network
{
    /// <summary>Maps steer/brake axes to the analog ZL (location 0) / ZR (location 1) paddle values of the Zwift Ride.</summary>
    internal static class PaddleMath
    {
        /// <summary>
        /// ZL: outward (negative) = steer left, inward (positive) = brake. ZR: outward (positive) = steer right, inward (negative) = brake.
        /// When steering and braking compete for the same paddle the larger magnitude wins (steer on ties).
        /// </summary>
        public static (sbyte Zl, sbyte Zr) Compute(double steer, double brake)
        {
            int s = Scale(steer, -100, 100);
            int b = Scale(brake, 0, 100);
            int left = Math.Min(s, 0);
            int right = Math.Max(s, 0);
            int zl = -left >= b ? left : b;
            int zr = right >= b ? right : -b;
            return ((sbyte)zl, (sbyte)zr);
        }

        private static int Scale(double v, int min, int max)
            => double.IsNaN(v) ? 0 : (int)Math.Clamp(Math.Round(100 * v, MidpointRounding.AwayFromZero), min, max);
    }
}
