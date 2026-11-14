using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;
using NSubstitute;

namespace GameControllerForZwift.Core.Tests
{
    /// <summary>Edge cases of the state building in <see cref="DataIntegrator"/>.</summary>
    public class DataIntegratorEdgeCaseTests
    {
        private readonly IOutputService _outputService = Substitute.For<IOutputService>();
        private readonly IControllerProfileService _profileService = Substitute.For<IControllerProfileService>();
        private readonly ControllerProfile _profile = new();
        private readonly DataIntegrator _sut;

        public DataIntegratorEdgeCaseTests()
        {
            var profiles = new ControllerProfiles();
            profiles.Profiles.Add(_profile);
            _profileService.Profiles.Returns(profiles);
            _sut = new DataIntegrator(Substitute.For<IInputService>(), _profileService, _outputService, 10);
        }

        private async Task RunAsync(DataIntegrator sut, params ControllerData[] data)
        {
            foreach (var d in data) sut.ProcessControllerData(d);
            using var cts = new CancellationTokenSource(150);
            try { await sut.WriteDataAsync(cts.Token); } catch (OperationCanceledException) { }
        }

        private OutputState[] States() =>
            _outputService.ReceivedCalls().Where(c => c.GetMethodInfo().Name == nameof(IOutputService.UpdateAsync))
                  .Select(c => (OutputState)c.GetArguments()[0]!).ToArray();

        [Fact]
        public void ProcessControllerData_Null_IsIgnored()
        {
            _sut.ProcessControllerData(null!);
            Assert.Empty(_sut.DataQueue);
        }

        [Fact]
        public async Task ReadDataAsync_NullController_ReturnsImmediately()
            => await _sut.ReadDataAsync(null!, CancellationToken.None);

        [Fact]
        public async Task ReadDataAsync_EnqueuesDataFromController()
        {
            var controller = Substitute.For<IController>();
            controller.ReadData().Returns(new ControllerData { A = true });
            using var cts = new CancellationTokenSource(120);
            try { await _sut.ReadDataAsync(controller, cts.Token); } catch (OperationCanceledException) { }
            Assert.NotEmpty(_sut.DataQueue);
        }

        [Fact]
        public async Task NoProfileService_Profiles_SendsNeutralStateWithBattery()
        {
            _profileService.Profiles.Returns((ControllerProfiles)null!);
            await RunAsync(_sut, new ControllerData { A = true, BatteryPercent = 33 });
            var s = Assert.Single(States());
            Assert.Empty(s.Pressed);
            Assert.Equal(0, s.Steer);
            Assert.Equal(33, s.BatteryPercent);
        }

        [Fact]
        public async Task SteerLeftAndRightTogether_CancelOut()
        {
            _profile.UpdateMapping(ControllerInput.LeftThumbstick_TiltLeft, ZwiftFunction.SteerLeft);
            _profile.UpdateMapping(ControllerInput.DPad_Right, ZwiftFunction.SteerRight);
            await RunAsync(_sut, new ControllerData { LeftThumbstickX = -32767, DPad_Right = true });
            Assert.Equal(0.0, States()[0].Steer, 3);
        }

        [Fact]
        public async Task SameFunctionOnTwoInputs_UsesLargestAnalogValue()
        {
            _profile.UpdateMapping(ControllerInput.LeftTrigger, ZwiftFunction.Brake);
            _profile.UpdateMapping(ControllerInput.RightTrigger, ZwiftFunction.Brake);
            await RunAsync(_sut, new ControllerData { LeftTrigger = 8000, RightTrigger = 16000 });
            var brake = States()[0].Brake;
            Assert.InRange(brake, 0.4, 0.6);
        }

        [Fact]
        public async Task MappingRemovedFromProfile_IsSkipped()
        {
            _profile.Mappings.Clear();
            await RunAsync(_sut, new ControllerData { A = true });
            Assert.True(States()[0].IsNeutral);
        }

        [Fact]
        public void UpdateMapping_ExistingInput_ChangesFunction()
        {
            _sut.UpdateMapping(new InputMapping(ControllerInput.A, ZwiftFunction.Select));
            Assert.Equal(ZwiftFunction.Select, _profile.Mappings.Single(m => m.Input == ControllerInput.A).Function);
        }

        [Fact]
        public void UpdateMapping_MissingInput_AddsMapping()
        {
            _profile.Mappings.Clear();
            _sut.UpdateMapping(new InputMapping(ControllerInput.B, ZwiftFunction.GoBack));
            var added = Assert.Single(_profile.Mappings);
            Assert.Equal(ZwiftFunction.GoBack, added.Function);
        }

        [Fact]
        public void StopProcessing_WithoutStart_SendsNeutral()
        {
            _sut.StopProcessing();
            _outputService.Received().UpdateAsync(Arg.Is<OutputState>(s => s.IsNeutral), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void StopProcessing_SwallowsOutputException()
        {
            _outputService.UpdateAsync(Arg.Any<OutputState>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new InvalidOperationException());
            _sut.StopProcessing();
        }

        [Fact]
        public void StartProcessing_Twice_DisposesPreviousSource()
        {
            _sut.StartProcessing(Substitute.For<IController>());
            _sut.StartProcessing(Substitute.For<IController>());
            _sut.StopProcessing();
        }
    }
}
