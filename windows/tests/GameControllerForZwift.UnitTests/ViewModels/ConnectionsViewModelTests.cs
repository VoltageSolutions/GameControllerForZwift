using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;
using static GameControllerForZwift.UI.WPF.Tests.ViewModels.ConfigurationTestContext;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>
    /// The Connections tab's view model: your controller (SCR-CONN-001 to SCR-CONN-003) and the Zwift steps
    /// (SCR-CONN-009 to SCR-CONN-011).
    /// </summary>
    public sealed class ConnectionsViewModelTests : IDisposable
    {
        private readonly ConfigurationTestContext _context = new();
        private readonly IZwiftConnection _connection = Substitute.For<IZwiftConnection>();
        private readonly List<string> _opened = [];
        private ZwiftStatus? _status;
        private ControllerSetupViewModel? _setup;

        public void Dispose()
        {
            _status?.Dispose();
            _context.Dispose();
        }

        private ConnectionsViewModel Create(bool zwiftConnected = false, params IController[] controllers)
        {
            _connection.IsZwiftConnected.Returns(zwiftConnected);
            _status = new ZwiftStatus(_connection);
            _setup = _context.CreateViewModel(controllers);
            return new ConnectionsViewModel(_status, _setup, _opened.Add);
        }

        private void RaiseZwift(bool connected) =>
            _connection.ConnectionChanged += NSubstitute.Raise.Event<EventHandler<bool>>(_connection, connected);

        [Fact]
        public void SCRCONN001_WithAController_SaysItsNameAndStateInWords()
        {
            ConnectionsViewModel viewModel = Create(false, Controller(Xbox));

            Assert.True(viewModel.HasController);
            Assert.Equal("Your Xbox Series X Controller is here", viewModel.ControllerText);
            Assert.Equal("Awake and connected", viewModel.ControllerCaption);
        }

        [Fact]
        public void SCRCONN003_WithNoController_AsksToWakeItAndSaysItIsLooking()
        {
            ConnectionsViewModel viewModel = Create();

            Assert.False(viewModel.HasController);
            Assert.Equal("Wake your controller", viewModel.ControllerText);
            Assert.Equal("Looking for your controller...", viewModel.ControllerCaption);
        }

        [Fact]
        public void SCRCONN001_ControllerTextFollowsTheSelectedController()
        {
            ConnectionsViewModel viewModel = Create();
            var changed = new List<string?>();
            viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            IController dualSense = Controller(DualSense);
            _context.DataIntegrator.GetControllers().Returns([dualSense]);
            _setup!.DetectControllers();

            Assert.Contains(nameof(ConnectionsViewModel.ControllerText), changed);
            Assert.Contains(nameof(ConnectionsViewModel.HasController), changed);
            Assert.Equal("Your DualSense Wireless Controller is here", viewModel.ControllerText);
        }

        [Fact]
        public void SCRCONN002_ThePickerListsTheControllersAndNeedsTwoToChange()
        {
            ConnectionsViewModel one = Create(false, Controller(Xbox));
            Assert.False(one.CanPickController);
            Assert.Single(one.Controllers);

            using var context = new ConfigurationTestContext();
            IController xbox = Controller(Xbox);
            IController dualSense = Controller(DualSense);
            ControllerSetupViewModel setup = context.CreateViewModel(xbox, dualSense);
            using var status = new ZwiftStatus();
            var two = new ConnectionsViewModel(status, setup, _opened.Add);

            Assert.True(two.CanPickController);
            Assert.Same(xbox, two.SelectedController);
        }

        [Fact]
        public void SCRCONN002_PickingAnotherControllerSelectsItOnTheButtonsTab()
        {
            using var context = new ConfigurationTestContext();
            IController xbox = Controller(Xbox);
            IController dualSense = Controller(DualSense);
            ControllerSetupViewModel setup = context.CreateViewModel(xbox, dualSense);
            using var status = new ZwiftStatus();
            var viewModel = new ConnectionsViewModel(status, setup, _opened.Add);
            var changed = new List<string?>();
            viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            viewModel.SelectedController = dualSense;

            Assert.Same(dualSense, setup.SelectedController);
            Assert.Contains(nameof(ConnectionsViewModel.SelectedController), changed);
            Assert.Equal("Your DualSense Wireless Controller is here", viewModel.ControllerText);
        }

        [Fact]
        public void SCRCONN002_PairingABluetoothControllerOpensWindowsBluetoothSettings()
        {
            ConnectionsViewModel viewModel = Create();

            viewModel.PairBluetoothCommand.Execute(null);

            Assert.Equal(["ms-settings:bluetooth"], _opened);
        }

        [Fact]
        public void SCRCONN002_BothWaysOutStayAvailableWithNoController()
        {
            ConnectionsViewModel viewModel = Create();

            Assert.True(viewModel.PairBluetoothCommand.CanExecute(null));
            Assert.Empty(viewModel.Controllers);
        }

        [Fact]
        public void SCRCONN009_NotConnected_ExpandsTheStepsWithNoneDone()
        {
            ConnectionsViewModel viewModel = Create();

            Assert.True(viewModel.ShowSteps);
            Assert.False(viewModel.ShowMeHowVisible);
            Assert.Equal(
                ["Power Source: Voltage Gamepad", "Controls: Voltage Gamepad", "Power Source: your trainer"],
                viewModel.Steps.Select(s => s.Text));
            Assert.All(viewModel.Steps, s =>
            {
                Assert.False(s.IsDone);
                Assert.Equal("Not done yet", s.StateText);
            });
        }

        [Fact]
        public void SCRCONN010_TheStepsNameVoltageGamepad()
        {
            ConnectionsViewModel viewModel = Create();

            Assert.Contains("Voltage Gamepad", viewModel.PowerSourceStep.Text, StringComparison.Ordinal);
            Assert.Contains("Voltage Gamepad", viewModel.ControlsStep.Text, StringComparison.Ordinal);
        }

        [Fact]
        public void SCRCONN009_Connected_TicksOnlyTheFirstStep()
        {
            ConnectionsViewModel viewModel = Create(true);

            Assert.True(viewModel.PowerSourceStep.IsDone);
            Assert.Equal("Done", viewModel.PowerSourceStep.StateText);
            Assert.False(viewModel.ControlsStep.IsDone);
            Assert.False(viewModel.TrainerStep.IsDone);
        }

        [Fact]
        public void SCRCONN009_StepOneTicksAndUnticksLiveWithTheConnection()
        {
            ConnectionsViewModel viewModel = Create();
            var changed = new List<string?>();
            viewModel.PowerSourceStep.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            RaiseZwift(true);
            Assert.True(viewModel.PowerSourceStep.IsDone);
            Assert.Contains(nameof(ZwiftStepViewModel.StateText), changed);

            RaiseZwift(false);
            Assert.False(viewModel.PowerSourceStep.IsDone);
            Assert.False(viewModel.ControlsStep.IsDone);
            Assert.False(viewModel.TrainerStep.IsDone);
        }

        [Fact]
        public void SCRCONN011_Connected_CollapsesTheStepsAndOffersShowMeHow()
        {
            ConnectionsViewModel viewModel = Create(true);

            Assert.False(viewModel.ShowSteps);
            Assert.True(viewModel.ShowMeHowVisible);
            Assert.Equal("Show me how", viewModel.ShowMeHowLabel);
        }

        [Fact]
        public void SCRCONN011_ShowMeHowExpandsTheSteps_AndTheSameButtonHidesThemAgain()
        {
            ConnectionsViewModel viewModel = Create(true);
            var changed = new List<string?>();
            viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            viewModel.ShowMeHowCommand.Execute(null);

            Assert.True(viewModel.ShowSteps);
            Assert.True(viewModel.ShowMeHowVisible);
            Assert.Equal("Hide steps", viewModel.ShowMeHowLabel);
            Assert.Contains(nameof(ConnectionsViewModel.ShowSteps), changed);
            Assert.Contains(nameof(ConnectionsViewModel.ShowMeHowLabel), changed);

            viewModel.ShowMeHowCommand.Execute(null);

            Assert.False(viewModel.ShowSteps);
            Assert.Equal("Show me how", viewModel.ShowMeHowLabel);
        }

        [Fact]
        public void SCRCONN011_StepsExpandWhenZwiftDisconnects_AndCollapseAgainOnTheNextConnection()
        {
            ConnectionsViewModel viewModel = Create(true);
            viewModel.ShowMeHowCommand.Execute(null);

            RaiseZwift(false);
            Assert.True(viewModel.ShowSteps);
            Assert.False(viewModel.ShowMeHowVisible);

            RaiseZwift(true);
            Assert.False(viewModel.ShowSteps);
            Assert.True(viewModel.ShowMeHowVisible);
            Assert.Equal("Show me how", viewModel.ShowMeHowLabel);
        }

        [Fact]
        public void SCRCONN004_ExposesTheZwiftStatus_AndNeedsItsParts()
        {
            ConnectionsViewModel viewModel = Create();

            Assert.Same(_status, viewModel.Zwift);
            Assert.Throws<ArgumentNullException>(() => new ConnectionsViewModel(null!, _setup!));
            Assert.Throws<ArgumentNullException>(() => new ConnectionsViewModel(_status!, null!));
        }
    }
}
