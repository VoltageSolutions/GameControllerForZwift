using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Localization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// View model of the Connections tab: your controller (SCR-CONN-001 to SCR-CONN-003) and the Zwift card with its
    /// three steps (SCR-CONN-004 to SCR-CONN-011).
    /// </summary>
    public sealed partial class ConnectionsViewModel : ObservableObject
    {
        /// <summary>The Windows settings page for Bluetooth devices.</summary>
        public const string BluetoothSettingsUri = "ms-settings:bluetooth";

        private readonly ControllerSetupViewModel _controllers;
        private readonly Action<string> _openUri;
        private bool _stepsExpanded;

        /// <summary>Follows Zwift and the connected controllers.</summary>
        /// <param name="zwiftStatus">Whether Zwift is connected.</param>
        /// <param name="controllers">The connected controllers and the selected one.</param>
        /// <param name="openUri">Opens a URI with Windows. Without one, the shell opens it.</param>
        public ConnectionsViewModel(ZwiftStatus zwiftStatus, ControllerSetupViewModel controllers, Action<string>? openUri = null)
        {
            ArgumentNullException.ThrowIfNull(zwiftStatus);
            ArgumentNullException.ThrowIfNull(controllers);
            Zwift = zwiftStatus;
            _controllers = controllers;
            _openUri = openUri ?? OpenWithShell;

            PowerSourceStep = new ZwiftStepViewModel("ConnectionsStep1", "ZwiftStepPowerSource", zwiftStatus.IsConnected);
            ControlsStep = new ZwiftStepViewModel("ConnectionsStep2", "ZwiftStepControls", false);
            TrainerStep = new ZwiftStepViewModel("ConnectionsStep3", "ZwiftStepTrainer", false);
            Steps = [PowerSourceStep, ControlsStep, TrainerStep];

            zwiftStatus.PropertyChanged += OnZwiftChanged;
            controllers.PropertyChanged += OnControllersChanged;
            controllers.Controllers.CollectionChanged += OnControllerListChanged;
        }

        /// <summary>Whether Zwift is connected, updated live.</summary>
        public ZwiftStatus Zwift { get; }

        /// <summary>The connected controllers, for the picker.</summary>
        public ObservableCollection<IController> Controllers => _controllers.Controllers;

        /// <summary>The selected controller. Choosing another one follows the unsaved-changes rules of the Buttons tab.</summary>
        public IController? SelectedController
        {
            get => _controllers.SelectedController;
            set => _controllers.SelectedController = value;
        }

        /// <summary>True when a controller is selected.</summary>
        public bool HasController => _controllers.SelectedController is not null;

        /// <summary>The controller's name and state in words (SCR-CONN-001), or "Wake your controller" without one (SCR-CONN-003).</summary>
        public string ControllerText => _controllers.SelectedController is { } controller
            ? Localizer.Format("ConnectionsControllerHere", controller.Name)
            : Localizer.Get("ConnectionsNoController");

        /// <summary>A second line about the controller's state.</summary>
        public string ControllerCaption => Localizer.Get(HasController ? "ConnectionsControllerAwake" : "ConnectionsLooking");

        /// <summary>True when there is another controller to choose (two or more are connected).</summary>
        public bool CanPickController => _controllers.Controllers.Count >= 2;

        /// <summary>The three Zwift steps, in order (SCR-CONN-009).</summary>
        public IReadOnlyList<ZwiftStepViewModel> Steps { get; }

        /// <summary>Step 1, ticked when Zwift is connected (SCR-SETUP-019).</summary>
        public ZwiftStepViewModel PowerSourceStep { get; }

        /// <summary>Step 2, never ticked because the app does not read the subscription (SCR-SETUP-020).</summary>
        public ZwiftStepViewModel ControlsStep { get; }

        /// <summary>Step 3, never ticked because the app cannot see it (SCR-SETUP-021).</summary>
        public ZwiftStepViewModel TrainerStep { get; }

        /// <summary>True when the steps show: always while Zwift is not connected, and after Show me how when it is (SCR-CONN-011).</summary>
        public bool ShowSteps => !Zwift.IsConnected || _stepsExpanded;

        /// <summary>True when the Show me how button is offered, which is while Zwift is connected.</summary>
        public bool ShowMeHowVisible => Zwift.IsConnected;

        /// <summary>The button's label: "Show me how" while the steps are collapsed, "Hide steps" while they are open.</summary>
        public string ShowMeHowLabel => Localizer.Get(_stepsExpanded ? "ConnectionsHideSteps" : "ConnectionsShowMeHow");

        [RelayCommand]
        private void ShowMeHow()
        {
            _stepsExpanded = !_stepsExpanded;
            RaiseStepsChanged();
        }

        [RelayCommand]
        private void PairBluetooth() => _openUri(BluetoothSettingsUri);

        private void OnZwiftChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ZwiftStatus.IsConnected))
            {
                return;
            }

            PowerSourceStep.IsDone = Zwift.IsConnected;
            if (!Zwift.IsConnected)
            {
                // The next connection starts collapsed again.
                _stepsExpanded = false;
            }

            RaiseStepsChanged();
        }

        private void OnControllersChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ControllerSetupViewModel.SelectedController))
            {
                OnPropertyChanged(nameof(SelectedController));
                OnPropertyChanged(nameof(HasController));
                OnPropertyChanged(nameof(ControllerText));
                OnPropertyChanged(nameof(ControllerCaption));
            }
        }

        private void OnControllerListChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            OnPropertyChanged(nameof(CanPickController));

        private void RaiseStepsChanged()
        {
            OnPropertyChanged(nameof(ShowSteps));
            OnPropertyChanged(nameof(ShowMeHowVisible));
            OnPropertyChanged(nameof(ShowMeHowLabel));
        }

        private static void OpenWithShell(string uri)
        {
            // A missing handler for the URI must not crash the app.
            try
            {
                using var process = Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                Debug.WriteLine($"Could not open {uri}: {ex.Message}");
            }
        }
    }

    /// <summary>One of the three Zwift steps,, with whether it is done in words as well as in its icon (SCR-CONN-009).</summary>
    public sealed partial class ZwiftStepViewModel : ObservableObject
    {
        private readonly string _textKey;

        public ZwiftStepViewModel(string automationId, string textKey, bool isDone)
        {
            AutomationId = automationId;
            _textKey = textKey;
            _isDone = isDone;
        }

        /// <summary>The automation ID of the step.</summary>
        public string AutomationId { get; }

        /// <summary>The instruction.</summary>
        public string Text => Localizer.Get(_textKey);

        /// <summary>True when the step is done.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        private bool _isDone;

        /// <summary>"Done" or "Not done yet".</summary>
        public string StateText => Localizer.Get(IsDone ? "ZwiftStepDone" : "ZwiftStepNotDone");
    }
}
