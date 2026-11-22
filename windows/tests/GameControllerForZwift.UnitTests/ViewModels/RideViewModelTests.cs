using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Navigation;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>The Ride tab's view model (docs/spec/screens/ride.md).</summary>
    public sealed class RideViewModelTests : IDisposable
    {
        private sealed class ManualTime : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 9, 3, 7, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan by) => _now += by;
        }

        private readonly ConfigurationTestContext _context = new();
        private readonly IZwiftConnection _connection = Substitute.For<IZwiftConnection>();
        private readonly INavigationService _navigation = Substitute.For<INavigationService>();
        private readonly ManualTime _time = new();
        private readonly List<string> _opened = [];
        private ZwiftStatus? _status;
        private ControllerSetupViewModel? _setup;
        private RideViewModel? _ride;

        public void Dispose()
        {
            _ride?.Dispose();
            _status?.Dispose();
            _context.Dispose();
        }

        private RideViewModel Create(bool zwiftConnected = false, bool controller = false)
        {
            _connection.IsZwiftConnected.Returns(zwiftConnected);
            _status = new ZwiftStatus(_connection);
            _setup = controller ? _context.CreateViewModel(Controller(Xbox)) : _context.CreateViewModel();
            var connections = new ConnectionsViewModel(_status, _setup, _opened.Add);
            _ride = new RideViewModel(_status, _setup, _context.Settings, _context.Dialogs, _navigation, connections, _time);
            return _ride;
        }

        private void RaiseZwift(bool connected) =>
            _connection.ConnectionChanged += Raise.Event<EventHandler<bool>>(_connection, connected);

        private void Press(ControllerInput input)
        {
            var data = new ControllerData();
            switch (input)
            {
                case ControllerInput.X:
                    data.X = true;
                    break;
                case ControllerInput.A:
                    data.A = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(input));
            }

            _setup!.CurrentControllerValues = data;
            _setup.CurrentControllerValues = new ControllerData();
        }

        private RideViewModel Riding()
        {
            RideViewModel ride = Create(zwiftConnected: true, controller: true);
            _setup!.ButtonFunctionMappings.First(m => m.SelectedInput == ControllerInput.X).SelectedZwiftFunction = ZwiftFunction.ShiftUp;
            return ride;
        }

        [Fact]
        public void SCRRIDE001_SCRRIDE002_HomeSaysReadyAndNamesTheController()
        {
            RideViewModel ride = Create(zwiftConnected: false, controller: true);

            Assert.True(ride.IsHome);
            Assert.Equal("Ready when you are", ride.TitleText);
            Assert.Equal("Your Xbox Series X Controller is here", ride.ControllerText);
            Assert.False(ride.ShowNoControllerActions);
        }

        [Fact]
        public void SCRRIDE008_SCRRIDE009_WithNoControllerHomeAsksToWakeItAndOffersTwoWaysOut()
        {
            RideViewModel ride = Create();

            Assert.Equal("Wake your controller", ride.TitleText);
            Assert.Equal("Looking for your controller...", ride.ControllerText);
            Assert.True(ride.ShowNoControllerActions);

            ride.GoToConnectionsCommand.Execute(null);
            ride.PairBluetoothCommand.Execute(null);

            _navigation.Received(1).NavigateToRoot(typeof(ConnectionsViewModel));
            Assert.Equal([ConnectionsViewModel.BluetoothSettingsUri], _opened);
        }

        [Fact]
        public void SCRRIDE011_TheTitleReturnsWhenTheControllerWakes()
        {
            RideViewModel ride = Create();
            var changed = new System.Collections.Concurrent.ConcurrentQueue<string?>();
            ride.PropertyChanged += (_, e) => changed.Enqueue(e.PropertyName);

            _setup!.DetectControllers();
            _setup.SelectedController = Controller(Xbox);

            Assert.Equal("Ready when you are", ride.TitleText);
            Assert.Contains(nameof(RideViewModel.TitleText), changed);
        }

        [Fact]
        public void SCRRIDE005_SCRRIDE012_SCRRIDE013_TheZwiftStatusFollowsZwiftAndNotConnectedShowsTheSteps()
        {
            RideViewModel ride = Create(zwiftConnected: false, controller: true);
            Assert.Equal("Zwift isn't connected yet", ride.ZwiftStatusText);
            Assert.True(ride.ShowPairWithZwift);
            Assert.Equal(3, ride.Steps.Count);

            RaiseZwift(true);

            Assert.Equal("Zwift is connected to Voltage Gamepad", ride.ZwiftStatusText);
            Assert.True(ride.IsZwiftConnected);
            Assert.False(ride.ShowPairWithZwift);
        }

        [Fact]
        public void SCRRIDE014_PairWithZwiftOpensConnections()
        {
            Create(controller: true).GoToConnectionsCommand.Execute(null);

            _navigation.Received(1).NavigateToRoot(typeof(ConnectionsViewModel));
        }

        [Fact]
        public void SCRRIDE004_TheMappingShowsAndChangeOpensButtons()
        {
            RideViewModel ride = Create(controller: true);

            Assert.Equal("Default", ride.MappingName);
            ride.ChangeMappingCommand.Execute(null);

            _navigation.Received(1).NavigateToRoot(typeof(ControllerSetupViewModel));
        }

        [Fact]
        public void SCRRIDE003_StatsAreHiddenUntilARideHasBeenCounted()
        {
            RideViewModel ride = Create();

            Assert.False(ride.HasStats);
            Assert.Equal(string.Empty, ride.RidesText);
        }

        [Fact]
        public void SCRRIDE003_StatsShowRealCountsWithSingularAndPlural()
        {
            _context.Settings.RecordRide(1, new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero));
            RideViewModel ride = Create();
            Assert.True(ride.HasStats);
            Assert.Equal("1 ride together", ride.RidesText);
            Assert.Equal("1 shift", ride.ShiftsText);
            Assert.StartsWith("Riding since ", ride.SinceText, StringComparison.Ordinal);

            _context.Settings.RecordRide(1085, new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero));
            RideViewModel again = new(_status!, _setup!, _context.Settings, _context.Dialogs, _navigation,
                new ConnectionsViewModel(_status!, _setup!, _opened.Add), _time);

            Assert.Equal("2 rides together", again.RidesText);
            Assert.Contains("1,086", again.ShiftsText, StringComparison.Ordinal);
            again.Dispose();
        }

        [Fact]
        public void SCRRIDE036_ARideStartsByItselfWhenBothAreConnected()
        {
            RideViewModel ride = Create(zwiftConnected: false, controller: true);
            Assert.True(ride.IsHome);

            RaiseZwift(true);

            Assert.True(ride.IsRiding);
            Assert.False(ride.IsHome);
            Assert.Equal("Nothing yet", ride.LastPressText);
        }

        [Fact]
        public void SCRRIDE016_SCRRIDE017_TheReadoutNamesTheLastPressAndShiftsAreCounted()
        {
            RideViewModel ride = Riding();

            Press(ControllerInput.X);
            Press(ControllerInput.A);
            Press(ControllerInput.X);

            Assert.StartsWith("Shift up, ", ride.LastPressText, StringComparison.Ordinal);
            Assert.Equal("Shifts this ride: 2", ride.ShiftsThisRideText);
            Assert.Contains("Shift up", Assert.Single(ride.QuickReference).Text, StringComparison.Ordinal);
        }

        [Fact]
        public void SCRRIDE016_AButtonWithNoFunctionIsNotShownInTheReadout()
        {
            RideViewModel ride = Riding();
            string before = ride.LastPressText;
            _setup!.ButtonFunctionMappings.First(m => m.SelectedInput == ControllerInput.A).SelectedZwiftFunction = ZwiftFunction.None;

            Press(ControllerInput.A);

            Assert.Equal(before, ride.LastPressText);
            Assert.Equal("Shifts this ride: 0", ride.ShiftsThisRideText);
        }

        [Fact]
        public void SCRRIDE023_SCRRIDE041_ZwiftLostSaysTheControllerIsFineAndTheRideContinues()
        {
            RideViewModel ride = Riding();

            RaiseZwift(false);

            Assert.True(ride.IsRiding);
            Assert.True(ride.HasProblem);
            Assert.Equal("Your controller is fine", ride.ProblemTitle);
            Assert.Contains("Voltage Gamepad", ride.ProblemBody, StringComparison.Ordinal);

            RaiseZwift(true);

            Assert.False(ride.HasProblem);
            Assert.True(ride.IsRiding);
        }

        [Fact]
        public void SCRRIDE026_SCRRIDE028_ControllerAsleepSaysSoAndBackSaysItIsBack()
        {
            RideViewModel ride = Riding();

            _setup!.SelectedController = null;

            Assert.True(ride.IsRiding);
            Assert.Equal("Your controller disconnected.", ride.ProblemTitle);
            Assert.Contains("Wake it up", ride.ProblemBody, StringComparison.Ordinal);

            _setup.SelectedController = Controller(Xbox);

            Assert.False(ride.HasProblem);
            Assert.Equal("Your controller is back", ride.RecoveryText);
        }

        [Fact]
        public void SCRRIDE037_TheRideEndsAfterTheTimeoutAndCountsInTheStats()
        {
            RideViewModel ride = Riding();
            Press(ControllerInput.X);
            _time.Advance(TimeSpan.FromMinutes(9));
            ride.CheckTimeout();
            Assert.True(ride.IsRiding);

            _time.Advance(TimeSpan.FromMinutes(1));
            ride.CheckTimeout();

            Assert.True(ride.IsFinished);
            Assert.Equal("Ride time: 0 min", ride.FinishedTimeText);
            Assert.Equal("Shifts: 1", ride.FinishedShiftsText);
            Assert.Equal("Rides together: 1", ride.FinishedRidesText);
            Assert.True(ride.HasStats);
            Assert.Equal("1 ride together", ride.RidesText);
        }

        [Fact]
        public void SCRRIDE037_TheSettingsTimeoutApplies()
        {
            RideViewModel ride = Riding();
            _context.Settings.SetRideTimeoutMinutes(30);
            _time.Advance(TimeSpan.FromMinutes(20));
            ride.CheckTimeout();
            Assert.True(ride.IsRiding);

            _time.Advance(TimeSpan.FromMinutes(10));
            ride.CheckTimeout();

            Assert.True(ride.IsFinished);
        }

        [Fact]
        public void SCRRIDE029_TheSummaryShowsHoursAndMinutes()
        {
            RideViewModel ride = Riding();
            _time.Advance(TimeSpan.FromMinutes(65));
            Press(ControllerInput.X);
            _time.Advance(TimeSpan.FromMinutes(10));
            ride.CheckTimeout();

            Assert.Equal("Ride time: 1 h 5 min", ride.FinishedTimeText);
        }

        [Fact]
        public void SCRRIDE030_SCRRIDE031_TheAskShowsOnceAndMaybeLaterDismissesIt()
        {
            RideViewModel ride = Riding();
            _time.Advance(TimeSpan.FromMinutes(10));
            ride.CheckTimeout();
            Assert.True(ride.ShowDonationAsk);

            ride.MaybeLaterCommand.Execute(null);
            Assert.False(ride.ShowDonationAsk);
            ride.DoneCommand.Execute(null);
            Assert.True(ride.IsHome);

            Press(ControllerInput.X);
            Assert.True(ride.IsRiding);
            _time.Advance(TimeSpan.FromMinutes(10));
            ride.CheckTimeout();

            Assert.True(ride.IsFinished);
            Assert.False(ride.ShowDonationAsk);
            Assert.True(_context.Settings.GetDonationAskShown());
        }

        [Fact]
        public void SCRRIDE039_ChipInOpensKofiAndClosesTheAsk()
        {
            RideViewModel ride = Riding();
            _time.Advance(TimeSpan.FromMinutes(10));
            ride.CheckTimeout();

            ride.ChipInCommand.Execute(null);

            Assert.Equal([ProjectLinks.Kofi], _context.Dialogs.OpenedUrls);
            Assert.False(ride.ShowDonationAsk);
        }

        [Fact]
        public void SCRRIDE037_TheAppClosingEndsTheRideAndCountsIt()
        {
            RideViewModel ride = Riding();
            Press(ControllerInput.X);

            ride.EndRideForExit();

            Assert.Equal(1, _context.Settings.GetRideStats().Rides);
            Assert.Equal(1, _context.Settings.GetRideStats().Shifts);
            ride.EndRideForExit();
            Assert.Equal(1, _context.Settings.GetRideStats().Rides);
        }

        [Fact]
        public void TheTimerCanStartTwiceAndStopsOnDispose()
        {
            GameControllerForZwift.UnitTests.TestApplication.Run(() =>
            {
                RideViewModel ride = Create(controller: true);

                ride.StartTimer();
                ride.StartTimer();
                ride.Dispose();
            });
        }

        [Fact]
        public void TheConstructorRefusesNulls()
        {
            Create();
            var connections = new ConnectionsViewModel(_status!, _setup!, _opened.Add);

            Assert.Throws<ArgumentNullException>(() => new RideViewModel(null!, _setup!, _context.Settings, _context.Dialogs, _navigation, connections));
            Assert.Throws<ArgumentNullException>(() => new RideViewModel(_status!, null!, _context.Settings, _context.Dialogs, _navigation, connections));
            Assert.Throws<ArgumentNullException>(() => new RideViewModel(_status!, _setup!, null!, _context.Dialogs, _navigation, connections));
            Assert.Throws<ArgumentNullException>(() => new RideViewModel(_status!, _setup!, _context.Settings, null!, _navigation, connections));
            Assert.Throws<ArgumentNullException>(() => new RideViewModel(_status!, _setup!, _context.Settings, _context.Dialogs, null!, connections));
            Assert.Throws<ArgumentNullException>(() => new RideViewModel(_status!, _setup!, _context.Settings, _context.Dialogs, _navigation, null!));
        }
    }
}
