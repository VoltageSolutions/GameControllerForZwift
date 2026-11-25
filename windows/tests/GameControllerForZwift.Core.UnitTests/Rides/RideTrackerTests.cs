using GameControllerForZwift.Core.Rides;

namespace GameControllerForZwift.Core.Tests.Rides
{
    public sealed class RideTrackerTests
    {
        private sealed class ManualTime : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 11, 22, 8, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan by) => _now += by;
        }

        private readonly ManualTime _time = new();
        private TimeSpan _timeout = TimeSpan.FromMinutes(10);

        private RideTracker Create() => new(_time, () => _timeout);

        private RideTracker Riding()
        {
            RideTracker tracker = Create();
            tracker.SetControllerConnected(true);
            tracker.SetZwiftConnected(true);
            return tracker;
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void SCRRIDE036_ARideDoesNotStartUntilBothAreConnected(bool controller, bool zwift)
        {
            RideTracker tracker = Create();

            tracker.SetControllerConnected(controller);
            tracker.SetZwiftConnected(zwift);

            Assert.False(tracker.IsRiding);
            Assert.Null(tracker.StartedAt);
        }

        [Fact]
        public void SCRRIDE036_ARideStartsWhenBothAreConnectedInEitherOrder()
        {
            foreach (bool controllerFirst in new[] { true, false })
            {
                RideTracker tracker = Create();
                int started = 0;
                tracker.Started += (_, _) => started++;

                if (controllerFirst)
                {
                    tracker.SetControllerConnected(true);
                    tracker.SetZwiftConnected(true);
                }
                else
                {
                    tracker.SetZwiftConnected(true);
                    tracker.SetControllerConnected(true);
                }

                Assert.True(tracker.IsRiding);
                Assert.Equal(_time.GetUtcNow(), tracker.StartedAt);
                Assert.Equal(1, started);
                Assert.True(tracker.ControllerConnected);
                Assert.True(tracker.ZwiftConnected);
            }
        }

        [Fact]
        public void SCRRIDE036_RepeatedConnectionReportsDoNothing()
        {
            RideTracker tracker = Riding();
            int started = 0;
            tracker.Started += (_, _) => started++;

            tracker.SetControllerConnected(true);
            tracker.SetZwiftConnected(true);

            Assert.Equal(0, started);
        }

        [Fact]
        public void SCRRIDE037_ARideEndsAfterTheTimeoutWithNoPress()
        {
            RideTracker tracker = Riding();
            _time.Advance(TimeSpan.FromMinutes(9));
            Assert.Null(tracker.CheckTimeout());

            _time.Advance(TimeSpan.FromMinutes(1));
            RideSummary? summary = tracker.CheckTimeout();

            Assert.NotNull(summary);
            Assert.Equal(TimeSpan.Zero, summary.Duration);
            Assert.False(tracker.IsRiding);
        }

        [Fact]
        public void SCRRIDE037_APressKeepsTheRideAliveAndTheRideLastsUntilTheLastPress()
        {
            RideTracker tracker = Riding();
            _time.Advance(TimeSpan.FromMinutes(8));
            tracker.RegisterPress(isShift: true);
            tracker.RegisterPress(isShift: false);
            tracker.RegisterPress(isShift: true);
            _time.Advance(TimeSpan.FromMinutes(9));
            Assert.Null(tracker.CheckTimeout());

            _time.Advance(TimeSpan.FromMinutes(1));
            RideSummary summary = tracker.CheckTimeout()!;

            Assert.Equal(TimeSpan.FromMinutes(8), summary.Duration);
            Assert.Equal(2, summary.Shifts);
        }

        [Fact]
        public void SCRRIDE041_AShortLossOfEitherConnectionDoesNotEndTheRide()
        {
            RideTracker tracker = Riding();
            tracker.SetZwiftConnected(false);
            _time.Advance(TimeSpan.FromMinutes(2));
            tracker.RegisterPress(isShift: false);
            tracker.SetControllerConnected(false);
            _time.Advance(TimeSpan.FromMinutes(2));
            tracker.SetControllerConnected(true);
            tracker.SetZwiftConnected(true);

            Assert.Null(tracker.CheckTimeout());
            Assert.True(tracker.IsRiding);
        }

        [Fact]
        public void SCRRIDE037_ALossOfAConnectionForTheTimeoutEndsTheRideAtTheMomentItWasLost()
        {
            RideTracker tracker = Riding();
            _time.Advance(TimeSpan.FromMinutes(3));
            tracker.SetZwiftConnected(false);
            _time.Advance(TimeSpan.FromMinutes(5));
            tracker.RegisterPress(isShift: false);
            _time.Advance(TimeSpan.FromMinutes(10));

            RideSummary summary = tracker.CheckTimeout()!;

            Assert.Equal(TimeSpan.FromMinutes(3), summary.Duration);
        }

        [Fact]
        public void SCRRIDE037_TheTimeoutIsReadEachTimeSoASettingsChangeApplies()
        {
            RideTracker tracker = Riding();
            _time.Advance(TimeSpan.FromMinutes(6));
            Assert.Null(tracker.CheckTimeout());

            _timeout = TimeSpan.FromMinutes(5);

            Assert.NotNull(tracker.CheckTimeout());
        }

        [Fact]
        public void SCRRIDE037_TheAppClosingEndsTheRideNow()
        {
            RideTracker tracker = Riding();
            _time.Advance(TimeSpan.FromMinutes(4));
            tracker.RegisterPress(isShift: true);

            RideSummary summary = tracker.End()!;

            Assert.Equal(TimeSpan.FromMinutes(4), summary.Duration);
            Assert.Equal(1, summary.Shifts);
            Assert.Null(tracker.End());
        }

        [Fact]
        public void SCRRIDE037_NoRideMeansNothingToCheck()
        {
            RideTracker tracker = Create();

            Assert.Null(tracker.CheckTimeout());
            Assert.Null(tracker.End());
            tracker.RegisterPress(isShift: true);
            Assert.Equal(0, tracker.Shifts);
        }

        [Fact]
        public void AfterARideEndsWithBothConnectedTheNextPressStartsANewRide()
        {
            RideTracker tracker = Riding();
            _time.Advance(TimeSpan.FromMinutes(10));
            tracker.CheckTimeout();
            Assert.False(tracker.IsRiding);

            tracker.RegisterPress(isShift: true);

            Assert.True(tracker.IsRiding);
            Assert.Equal(1, tracker.Shifts);
        }

        [Fact]
        public void AfterARideEndsAReconnectStartsANewRide()
        {
            RideTracker tracker = Riding();
            _time.Advance(TimeSpan.FromMinutes(10));
            tracker.CheckTimeout();

            tracker.SetZwiftConnected(false);
            tracker.SetZwiftConnected(true);

            Assert.True(tracker.IsRiding);
        }

        [Fact]
        public void ARideThatEndedBecauseAConnectionWasLostWaitsForBothToComeBack()
        {
            RideTracker tracker = Riding();
            tracker.SetControllerConnected(false);
            _time.Advance(TimeSpan.FromMinutes(10));
            tracker.CheckTimeout();

            tracker.RegisterPress(isShift: false);
            Assert.False(tracker.IsRiding);

            tracker.SetControllerConnected(true);
            Assert.True(tracker.IsRiding);
        }

        [Fact]
        public void TheConstructorRefusesNulls()
        {
            Assert.Throws<ArgumentNullException>(() => new RideTracker(null!, () => TimeSpan.Zero));
            Assert.Throws<ArgumentNullException>(() => new RideTracker(TimeProvider.System, null!));
        }

        [Fact]
        public void RideStatsStartEmpty()
        {
            Assert.Equal(new RideStats(0, 0, null), RideStats.None);
        }
    }
}
