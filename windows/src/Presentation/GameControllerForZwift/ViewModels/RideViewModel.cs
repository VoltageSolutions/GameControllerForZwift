using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Rides;
using GameControllerForZwift.UI.WPF.Dialogs;
using GameControllerForZwift.UI.WPF.Localization;
using GameControllerForZwift.UI.WPF.Navigation;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>What the Ride tab shows.</summary>
    public enum RideMode
    {
        /// <summary>Home: ready, or what is missing (SCR-RIDE-001 to SCR-RIDE-015).</summary>
        Home,

        /// <summary>The riding view (SCR-RIDE-016 to SCR-RIDE-028).</summary>
        Riding,

        /// <summary>The summary after a ride (SCR-RIDE-029 to SCR-RIDE-032).</summary>
        Finished,
    }

    /// <summary>One line of the quick reference of the main buttons (SCR-RIDE-017).</summary>
    /// <param name="Text">For example "Right bumper: Shift up", for screen readers.</param>
    /// <param name="Chip">The short label of the input, for example "RB".</param>
    /// <param name="Function">The function's name, for example "Shift up".</param>
    public sealed record QuickReferenceItem(string Text, string Chip, string Function);

    /// <summary>
    /// View model of the Ride tab (docs/spec/screens/ride.md). It shows Home until a controller and Zwift are both
    /// connected, then the riding view, then a summary when the ride ends.
    /// </summary>
    public sealed partial class RideViewModel : ObservableObject, IDisposable
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

        private readonly ZwiftStatus _zwift;
        private readonly ControllerSetupViewModel _controllers;
        private readonly IAppSettingsStore _settings;
        private readonly IDialogService _dialogs;
        private readonly INavigationService _navigation;
        private readonly ConnectionsViewModel _connections;
        private readonly RideTracker _tracker;
        private readonly HashSet<ControllerInput> _pressed = [];
        private DispatcherTimer? _timer;

        public RideViewModel(ZwiftStatus zwiftStatus, ControllerSetupViewModel controllers, IAppSettingsStore appSettings,
            IDialogService dialogService, INavigationService navigationService, ConnectionsViewModel connections, TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(zwiftStatus);
            ArgumentNullException.ThrowIfNull(controllers);
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(dialogService);
            ArgumentNullException.ThrowIfNull(navigationService);
            ArgumentNullException.ThrowIfNull(connections);
            _connections = connections;
            _zwift = zwiftStatus;
            _controllers = controllers;
            _settings = appSettings;
            _dialogs = dialogService;
            _navigation = navigationService;
            _tracker = new RideTracker(timeProvider ?? TimeProvider.System, () => TimeSpan.FromMinutes(_settings.GetRideTimeoutMinutes()));
            _tracker.Started += OnRideStarted;

            Steps =
            [
                Localizer.Get("ZwiftStepPowerSource"),
                Localizer.Get("ZwiftStepControls"),
                Localizer.Get("ZwiftStepTrainer"),
            ];

            _zwift.PropertyChanged += OnZwiftChanged;
            _controllers.PropertyChanged += OnControllersChanged;
            _tracker.SetZwiftConnected(_zwift.IsConnected);
            _tracker.SetControllerConnected(HasController);
            RefreshStats();
        }

        /// <summary>What the tab shows now.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsHome), nameof(IsRiding), nameof(IsFinished))]
        private RideMode _mode = RideMode.Home;

        /// <summary>True on Home.</summary>
        public bool IsHome => Mode == RideMode.Home;

        /// <summary>True in the riding view.</summary>
        public bool IsRiding => Mode == RideMode.Riding;

        /// <summary>True on the ride summary.</summary>
        public bool IsFinished => Mode == RideMode.Finished;

        #region Home

        /// <summary>True when a controller is selected.</summary>
        public bool HasController => _controllers.SelectedController is not null;

        /// <summary>The title: "Ready when you are", or "Wake your controller" without a controller (SCR-RIDE-001, SCR-RIDE-008).</summary>
        public string TitleText => Localizer.Get(HasController ? "RideTitle" : "RideNoControllerTitle");

        /// <summary>The controller's name and state in words (SCR-RIDE-002), or the search status without one.</summary>
        public string ControllerText => _controllers.SelectedController is { } controller
            ? Localizer.Format("RideControllerHere", controller.Name)
            : Localizer.Get("RideNoControllerStatus");

        /// <summary>The line over the controller's state, for example "Your Xbox Controller" (SCR-RIDE-002).</summary>
        public string ControllerName => _controllers.SelectedController is { } controller
            ? Localizer.Format("RideControllerName", controller.Name)
            : Localizer.Get("RideNoControllerStatus");

        /// <summary>The controller's state in words, or empty without a controller (SCR-RIDE-002).</summary>
        public string ControllerStateText => HasController ? Localizer.Get("RideControllerAwake") : string.Empty;

        /// <summary>The Zwift status in words (SCR-RIDE-005, SCR-RIDE-012).</summary>
        public string ZwiftStatusText => _zwift.StatusText;

        /// <summary>True while Zwift is connected.</summary>
        public bool IsZwiftConnected => _zwift.IsConnected;

        /// <summary>True without a controller, when Home offers the two ways out (SCR-RIDE-009, SCR-RIDE-040).</summary>
        public bool ShowNoControllerActions => !HasController;

        /// <summary>True when Zwift is not connected, so Home shows the three steps and Pair with Zwift (SCR-RIDE-013).</summary>
        public bool ShowPairWithZwift => !_zwift.IsConnected;

        /// <summary>The three Zwift steps in short, in order (SCR-RIDE-013).</summary>
        public IReadOnlyList<string> Steps { get; }

        /// <summary>The name of the selected mapping (SCR-RIDE-004).</summary>
        public string MappingName => _controllers.SelectedConfiguration?.Name ?? string.Empty;

        /// <summary>Opens the Connections tab, where the pairing steps and the controller picker are (SCR-RIDE-009, SCR-RIDE-014).</summary>
        [RelayCommand]
        private void GoToConnections() => _navigation.NavigateToRoot(typeof(ConnectionsViewModel));

        /// <summary>Opens the Windows Bluetooth settings to pair a new controller (SCR-RIDE-009).</summary>
        [RelayCommand]
        private void PairBluetooth() => _connections.PairBluetoothCommand.Execute(null);

        /// <summary>Opens the Buttons tab so the mapping can change (SCR-RIDE-004).</summary>
        [RelayCommand]
        private void ChangeMapping() => _navigation.NavigateToRoot(typeof(ControllerSetupViewModel));

        #endregion

        #region Stats

        /// <summary>True when the app has counted at least one ride, so the stats show (SCR-RIDE-003).</summary>
        [ObservableProperty]
        private bool _hasStats;

        /// <summary>The number of rides, for example "12" (SCR-RIDE-003).</summary>
        [ObservableProperty]
        private string _ridesValue = string.Empty;

        /// <summary>The label of the number of rides, for example "rides together".</summary>
        [ObservableProperty]
        private string _ridesLabel = string.Empty;

        /// <summary>The number of shifts, for example "1,086".</summary>
        [ObservableProperty]
        private string _shiftsValue = string.Empty;

        /// <summary>The label of the number of shifts, for example "shifts".</summary>
        [ObservableProperty]
        private string _shiftsLabel = string.Empty;

        /// <summary>The month the app first counted a ride, for example "Sep".</summary>
        [ObservableProperty]
        private string _sinceValue = string.Empty;

        /// <summary>The label of the month, "riding since".</summary>
        [ObservableProperty]
        private string _sinceLabel = string.Empty;

        /// <summary>For example "12 rides together".</summary>
        [ObservableProperty]
        private string _ridesText = string.Empty;

        /// <summary>For example "1,086 shifts".</summary>
        [ObservableProperty]
        private string _shiftsText = string.Empty;

        /// <summary>For example "Riding since September 2026".</summary>
        [ObservableProperty]
        private string _sinceText = string.Empty;

        private void RefreshStats()
        {
            RideStats stats = _settings.GetRideStats();
            HasStats = stats.Rides > 0;
            if (!HasStats)
            {
                RidesText = ShiftsText = SinceText = string.Empty;
                RidesValue = RidesLabel = ShiftsValue = ShiftsLabel = SinceValue = SinceLabel = string.Empty;
                return;
            }

            RidesValue = stats.Rides.ToString("N0", Localizer.Instance.Culture);
            RidesLabel = Localizer.Get(stats.Rides == 1 ? "RideStatRidesOne" : "RideStatRidesOther");
            ShiftsValue = stats.Shifts.ToString("N0", Localizer.Instance.Culture);
            ShiftsLabel = Localizer.Get(stats.Shifts == 1 ? "RideStatShiftsOne" : "RideStatShiftsOther");
            SinceValue = stats.Since is { } first ? first.LocalDateTime.ToString("MMM", Localizer.Instance.Culture) : string.Empty;
            SinceLabel = stats.Since is null ? string.Empty : Localizer.Get("RideStatSince");

            RidesText = Count("RideStatsRidesOne", "RideStatsRidesOther", stats.Rides);
            ShiftsText = Count("RideStatsShiftsOne", "RideStatsShiftsOther", stats.Shifts);
            SinceText = stats.Since is { } since
                ? Localizer.Format("RideStatsSince", since.LocalDateTime.ToString("MMMM yyyy", Localizer.Instance.Culture))
                : string.Empty;
        }

        private static string Count(string oneKey, string otherKey, int count) =>
            count == 1 ? Localizer.Get(oneKey) : Localizer.Format(otherKey, count.ToString("N0", Localizer.Instance.Culture));

        #endregion

        #region Riding

        /// <summary>The last press in words, for example "Shift up, right bumper", for screen readers (SCR-RIDE-016).</summary>
        [ObservableProperty]
        private string _lastPressText = Localizer.Get("RideNoPress");

        /// <summary>The short label of the last input pressed, for example "RB", or empty before a press (SCR-RIDE-016).</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLastPress))]
        private string _lastPressKey = string.Empty;

        /// <summary>The big readout of the last press: the function's name, for example "Shift up" (SCR-RIDE-016).</summary>
        [ObservableProperty]
        private string _lastPressAction = Localizer.Get("RideNoPress");

        /// <summary>True once a press shows in the readout.</summary>
        public bool HasLastPress => LastPressKey.Length > 0;

        /// <summary>The controller chip of the riding view: ready or disconnected (SCR-RIDE-018).</summary>
        public string ControllerChipText => Localizer.Get(HasController ? "RideChipControllerReady" : "RideChipControllerGone");

        /// <summary>The Zwift chip of the riding view: seen or lost (SCR-RIDE-018).</summary>
        public string ZwiftChipText => Localizer.Get(_zwift.IsConnected ? "RideChipZwiftSees" : "RideChipZwiftLost");

        /// <summary>"3 shifts this ride" (SCR-RIDE-017).</summary>
        [ObservableProperty]
        private string _shiftsThisRideText = ShiftsThisRide(0);

        /// <summary>The shift buttons of the current mapping (SCR-RIDE-017).</summary>
        [ObservableProperty]
        private IReadOnlyList<QuickReferenceItem> _quickReference = [];

        /// <summary>The title of the problem the riding view shows, or empty (SCR-RIDE-023, SCR-RIDE-026).</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasProblem))]
        private string _problemTitle = string.Empty;

        /// <summary>What to do about the problem.</summary>
        [ObservableProperty]
        private string _problemBody = string.Empty;

        /// <summary>Says the controller is back, until the next press (SCR-RIDE-028).</summary>
        [ObservableProperty]
        private string _recoveryText = string.Empty;

        /// <summary>True while the riding view shows a problem.</summary>
        public bool HasProblem => ProblemTitle.Length > 0;

        private void OnRideStarted(object? sender, EventArgs e)
        {
            Mode = RideMode.Riding;
            LastPressText = Localizer.Get("RideNoPress");
            LastPressKey = string.Empty;
            LastPressAction = Localizer.Get("RideNoPress");
            RecoveryText = string.Empty;
            UpdateShifts();
            UpdateQuickReference();
            UpdateProblem();
        }

        private void UpdateShifts() => ShiftsThisRideText = ShiftsThisRide(_tracker.Shifts);

        private static string ShiftsThisRide(int shifts) =>
            Localizer.Format(shifts == 1 ? "RideShiftsThisRideOne" : "RideShiftsThisRideOther", shifts.ToString("N0", Localizer.Instance.Culture));

        private void UpdateQuickReference()
        {
            IReadOnlyDictionary<ControllerInput, ZwiftFunction> mappings = _controllers.GetCurrentMappings();
            QuickReference = ButtonsCatalog.Inputs
                .Where(i => mappings.TryGetValue(i.Input, out ZwiftFunction f) && f is ZwiftFunction.ShiftUp or ZwiftFunction.ShiftDown)
                .Select(i => new QuickReferenceItem(
                    Localizer.Format("RideQuickReferenceFormat", Localizer.Get(i.NameKey), Localizer.Get(ButtonsCatalog.Function(mappings[i.Input]).TitleKey)),
                    Localizer.Get(i.ChipKey),
                    Localizer.Get(ButtonsCatalog.Function(mappings[i.Input]).TitleKey)))
                .ToList();
        }

        private void UpdateProblem()
        {
            if (!_tracker.IsRiding)
            {
                return;
            }

            bool hadProblem = HasProblem;
            bool controllerGone = !HasController;
            if (controllerGone)
            {
                ProblemTitle = Localizer.Get("RideControllerAsleepTitle");
                ProblemBody = Localizer.Get("RideControllerAsleepBody");
            }
            else if (!_zwift.IsConnected)
            {
                ProblemTitle = Localizer.Get("RideZwiftLostTitle");
                ProblemBody = Localizer.Get("RideZwiftLostBody");
            }
            else
            {
                ProblemTitle = string.Empty;
                ProblemBody = string.Empty;
                if (hadProblem && HasController)
                {
                    RecoveryText = Localizer.Get("RideControllerBack");
                }
            }
        }

        private void DetectPresses(ControllerData? data)
        {
            if (data is null)
            {
                return;
            }

            IReadOnlyDictionary<ControllerInput, ZwiftFunction>? mappings = null;
            foreach (ControllerInput input in Enum.GetValues<ControllerInput>())
            {
                bool down = data.IsPressed(input);
                if (!down)
                {
                    _pressed.Remove(input);
                    continue;
                }

                if (!_pressed.Add(input))
                {
                    continue;
                }

                mappings ??= _controllers.GetCurrentMappings();
                ZwiftFunction function = mappings.TryGetValue(input, out ZwiftFunction f) ? f : ZwiftFunction.None;
                _tracker.RegisterPress(function is ZwiftFunction.ShiftUp or ZwiftFunction.ShiftDown);
                if (_tracker.IsRiding)
                {
                    if (function == ZwiftFunction.None)
                    {
                        continue;
                    }

                    LastPressKey = Localizer.Get(ButtonsCatalog.Input(input).ChipKey);
                    LastPressAction = Localizer.Get(ButtonsCatalog.Function(function).TitleKey);
                    LastPressText = Localizer.Format("RidePressFormat",
                        Localizer.Get(ButtonsCatalog.Function(function).TitleKey),
                        Localizer.Get(ButtonsCatalog.Input(input).NameKey).ToLower(Localizer.Instance.Culture));
                    RecoveryText = string.Empty;
                    UpdateShifts();
                    UpdateQuickReference();
                }
            }
        }

        #endregion

        #region Ride finished

        /// <summary>The ride time, for example "1 h 5 min" (SCR-RIDE-029).</summary>
        [ObservableProperty]
        private string _finishedTimeValue = string.Empty;

        /// <summary>The label under the ride time: "ride time".</summary>
        [ObservableProperty]
        private string _finishedTimeLabel = string.Empty;

        /// <summary>The shifts of the ride that ended.</summary>
        [ObservableProperty]
        private string _finishedShiftsValue = string.Empty;

        /// <summary>The label under the shifts: "shift" or "shifts".</summary>
        [ObservableProperty]
        private string _finishedShiftsLabel = string.Empty;

        /// <summary>The rides together, counting the ride that ended.</summary>
        [ObservableProperty]
        private string _finishedRidesValue = string.Empty;

        /// <summary>The label under the rides together: "ride together" or "rides together".</summary>
        [ObservableProperty]
        private string _finishedRidesLabel = string.Empty;

        /// <summary>True when the one-time donation ask shows (SCR-RIDE-030).</summary>
        [ObservableProperty]
        private bool _showDonationAsk;

        /// <summary>Opens the Ko-fi page and closes the ask (SCR-RIDE-039).</summary>
        [RelayCommand]
        private void ChipIn()
        {
            _dialogs.OpenUrl(ProjectLinks.Kofi);
            ShowDonationAsk = false;
        }

        /// <summary>Closes the ask. It is not shown again (SCR-RIDE-031).</summary>
        [RelayCommand]
        private void MaybeLater() => ShowDonationAsk = false;

        /// <summary>Returns from the summary to Home.</summary>
        [RelayCommand]
        private void Done() => Mode = RideMode.Home;

        private void Finish(RideSummary summary)
        {
            _settings.RecordRide(summary.Shifts, summary.StartedAt);
            RefreshStats();
            RideStats stats = _settings.GetRideStats();
            FinishedTimeValue = FormatDuration(summary.Duration);
            FinishedTimeLabel = Localizer.Get("RideFinishedTimeLabel");
            FinishedShiftsValue = summary.Shifts.ToString("N0", Localizer.Instance.Culture);
            FinishedShiftsLabel = Localizer.Get(summary.Shifts == 1 ? "RideStatShiftsOne" : "RideStatShiftsOther");
            FinishedRidesValue = stats.Rides.ToString("N0", Localizer.Instance.Culture);
            FinishedRidesLabel = Localizer.Get(stats.Rides == 1 ? "RideStatRidesOne" : "RideStatRidesOther");
            ShowDonationAsk = !_settings.GetDonationAskShown();
            if (ShowDonationAsk)
            {
                // The ask is shown at most once, so it counts as shown the moment it appears (SCR-RIDE-030).
                _settings.SetDonationAskShown();
            }

            ProblemTitle = ProblemBody = string.Empty;
            Mode = RideMode.Finished;
        }

        private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
            ? Localizer.Format("RideDurationHoursMinutes", (int)duration.TotalHours, duration.Minutes)
            : Localizer.Format("RideDurationMinutes", (int)duration.TotalMinutes);

        #endregion

        #region Timer and events

        /// <summary>Starts checking the ride timeout. Call it on the UI thread.</summary>
        public void StartTimer()
        {
            if (_timer is not null)
            {
                return;
            }

            _timer = new DispatcherTimer { Interval = CheckInterval };
            _timer.Tick += (_, _) => CheckTimeout();
            _timer.Start();
        }

        /// <summary>Ends the ride when the ride timeout has passed (SCR-RIDE-037).</summary>
        public void CheckTimeout()
        {
            if (_tracker.CheckTimeout() is { } summary)
            {
                Finish(summary);
            }
        }

        /// <summary>Ends the ride because the app is closing, so it counts in the stats (SCR-RIDE-037).</summary>
        public void EndRideForExit()
        {
            if (_tracker.End() is { } summary)
            {
                _settings.RecordRide(summary.Shifts, summary.StartedAt);
            }
        }

        private void OnZwiftChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ZwiftStatus.IsConnected))
            {
                return;
            }

            OnPropertyChanged(nameof(ZwiftStatusText));
            OnPropertyChanged(nameof(IsZwiftConnected));
            OnPropertyChanged(nameof(ShowPairWithZwift));
            OnPropertyChanged(nameof(ZwiftChipText));
            _tracker.SetZwiftConnected(_zwift.IsConnected);
            UpdateProblem();
        }

        private void OnControllersChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ControllerSetupViewModel.SelectedController):
                    OnPropertyChanged(nameof(HasController));
                    OnPropertyChanged(nameof(TitleText));
                    OnPropertyChanged(nameof(ControllerText));
                    OnPropertyChanged(nameof(ControllerName));
                    OnPropertyChanged(nameof(ControllerStateText));
                    OnPropertyChanged(nameof(ControllerChipText));
                    OnPropertyChanged(nameof(ShowNoControllerActions));
                    _tracker.SetControllerConnected(HasController);
                    UpdateProblem();
                    break;
                case nameof(ControllerSetupViewModel.SelectedConfiguration):
                    OnPropertyChanged(nameof(MappingName));
                    if (_tracker.IsRiding)
                    {
                        UpdateQuickReference();
                    }

                    break;
                case nameof(ControllerSetupViewModel.CurrentControllerValues):
                    DetectPresses(_controllers.CurrentControllerValues);
                    break;
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _zwift.PropertyChanged -= OnZwiftChanged;
            _controllers.PropertyChanged -= OnControllersChanged;
        }

        #endregion
    }
}
