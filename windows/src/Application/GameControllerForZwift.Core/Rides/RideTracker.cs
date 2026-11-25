namespace GameControllerForZwift.Core.Rides
{
    /// <summary>
    /// Decides when a ride starts and ends (docs/spec/screens/ride.md). A ride starts by itself when a controller and
    /// Zwift are both connected (SCR-RIDE-036). A short loss of either connection does not end it (SCR-RIDE-041). It
    /// ends when the ride timeout passes with no press or with either connection missing, or when the app closes
    /// (SCR-RIDE-037). The class has no timer: the caller asks <see cref="CheckTimeout"/> now and then.
    /// </summary>
    public sealed class RideTracker
    {
        private readonly TimeProvider _time;
        private readonly Func<TimeSpan> _timeout;
        private bool _controllerConnected;
        private bool _zwiftConnected;
        private DateTimeOffset? _controllerLostAt;
        private DateTimeOffset? _zwiftLostAt;
        private DateTimeOffset _lastPressAt;
        private DateTimeOffset? _startedAt;
        private bool _waitingForPress;

        /// <summary>Creates a tracker.</summary>
        /// <param name="timeProvider">The clock.</param>
        /// <param name="timeout">Returns the ride timeout, read each time so a change in Settings applies at once.</param>
        public RideTracker(TimeProvider timeProvider, Func<TimeSpan> timeout)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentNullException.ThrowIfNull(timeout);
            _time = timeProvider;
            _timeout = timeout;
        }

        /// <summary>Raised when a ride starts.</summary>
        public event EventHandler? Started;

        /// <summary>True while a ride is on.</summary>
        public bool IsRiding => _startedAt is not null;

        /// <summary>When the current ride started, or null when there is none.</summary>
        public DateTimeOffset? StartedAt => _startedAt;

        /// <summary>The shifts sent during the current ride.</summary>
        public int Shifts { get; private set; }

        /// <summary>True when the controller is connected.</summary>
        public bool ControllerConnected => _controllerConnected;

        /// <summary>True when Zwift is connected.</summary>
        public bool ZwiftConnected => _zwiftConnected;

        /// <summary>Tells the tracker whether a controller is connected.</summary>
        public void SetControllerConnected(bool connected)
        {
            if (connected == _controllerConnected)
            {
                return;
            }

            _controllerConnected = connected;
            _controllerLostAt = connected ? null : _time.GetUtcNow();
            _waitingForPress = false;
            TryStart();
        }

        /// <summary>Tells the tracker whether Zwift is connected.</summary>
        public void SetZwiftConnected(bool connected)
        {
            if (connected == _zwiftConnected)
            {
                return;
            }

            _zwiftConnected = connected;
            _zwiftLostAt = connected ? null : _time.GetUtcNow();
            _waitingForPress = false;
            TryStart();
        }

        /// <summary>
        /// Records a button press. It keeps the ride alive and counts as a shift when <paramref name="isShift"/> is true.
        /// After a ride ended with both still connected, the next press starts a new ride.
        /// </summary>
        public void RegisterPress(bool isShift)
        {
            if (_waitingForPress)
            {
                _waitingForPress = false;
                TryStart();
            }

            if (!IsRiding)
            {
                return;
            }

            _lastPressAt = _time.GetUtcNow();
            if (isShift)
            {
                Shifts++;
            }
        }

        /// <summary>Ends the ride when the ride timeout has passed. Returns its summary, or null when the ride goes on.</summary>
        public RideSummary? CheckTimeout()
        {
            if (_startedAt is null)
            {
                return null;
            }

            DateTimeOffset now = _time.GetUtcNow();
            TimeSpan timeout = _timeout();
            DateTimeOffset? endsAt = null;
            Consider(_lastPressAt);
            Consider(_controllerLostAt);
            Consider(_zwiftLostAt);
            return endsAt is { } at ? Finish(at) : null;

            void Consider(DateTimeOffset? since)
            {
                if (since is { } s && now - s >= timeout && (endsAt is null || s < endsAt))
                {
                    endsAt = s;
                }
            }
        }

        /// <summary>Ends the ride now, because the app is closing. Returns its summary, or null when there is no ride.</summary>
        public RideSummary? End() => _startedAt is null ? null : Finish(_time.GetUtcNow());

        private RideSummary Finish(DateTimeOffset at)
        {
            DateTimeOffset started = _startedAt!.Value;
            var summary = new RideSummary(started, at > started ? at - started : TimeSpan.Zero, Shifts);
            _startedAt = null;
            Shifts = 0;
            _waitingForPress = _controllerConnected && _zwiftConnected;
            return summary;
        }

        private void TryStart()
        {
            if (_startedAt is not null || _waitingForPress || !_controllerConnected || !_zwiftConnected)
            {
                return;
            }

            _startedAt = _time.GetUtcNow();
            _lastPressAt = _startedAt.Value;
            Shifts = 0;
            Started?.Invoke(this, EventArgs.Empty);
        }
    }
}
