using NSubstitute;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Tests
{
    public class DataIntegratorOutputTests : IDisposable
    {
        private readonly IOutputService _outputService = Substitute.For<IOutputService>();
        private readonly IControllerProfileService _profileService = Substitute.For<IControllerProfileService>();
        private readonly ControllerProfile _profile = new ControllerProfile();
        private readonly DataIntegrator _sut;

        public DataIntegratorOutputTests()
        {
            var profiles = new ControllerProfiles();
            profiles.Profiles.Add(_profile);
            _profileService.Profiles.Returns(profiles);
            _sut = new DataIntegrator(Substitute.For<IInputService>(), _profileService, _outputService, 10);
        }

        private async Task RunAsync(params ControllerData[] data)
        {
            foreach (var d in data) _sut.ProcessControllerData(d);
            using var cts = new CancellationTokenSource(150);
            try { await _sut.WriteDataAsync(cts.Token); } catch (OperationCanceledException) { }
        }

        private OutputState[] States() =>
            _outputService.ReceivedCalls().Where(c => c.GetMethodInfo().Name == nameof(IOutputService.UpdateAsync))
                  .Select(c => (OutputState)c.GetArguments()[0]!).ToArray();

        public void Dispose()
        {
            _sut.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task SteerRight_And_SteerLeft_ProduceSignedSteer()
        {
            _profile.UpdateMapping(ControllerInput.LeftThumbstickTiltLeft, ZwiftFunction.SteerLeft);
            _profile.UpdateMapping(ControllerInput.LeftThumbstickTiltRight, ZwiftFunction.SteerRight);

            await RunAsync(new ControllerData { LeftThumbstickX = 32767 }, new ControllerData { LeftThumbstickX = -32767 });

            var states = States();
            Assert.Equal(1.0, states[0].Steer, 6);
            Assert.Equal(-1.0, states[1].Steer, 6);
            Assert.Empty(states[0].Pressed);
        }

        [Fact]
        public async Task Brake_Trigger_IsAnalog_AndShiftIsPressed()
        {
            _profile.UpdateMapping(ControllerInput.RightTrigger, ZwiftFunction.Brake);
            _profile.UpdateMapping(ControllerInput.A, ZwiftFunction.ShiftUp);

            await RunAsync(new ControllerData { RightTrigger = 32767, A = true, BatteryPercent = 70 });

            var s = States()[0];
            Assert.Equal(1.0, s.Brake, 6);
            Assert.Contains(ZwiftFunction.ShiftUp, s.Pressed);
            Assert.DoesNotContain(ZwiftFunction.Brake, s.Pressed);
            Assert.Equal(70, s.BatteryPercent);
        }

        [Fact]
        public async Task Release_SendsNeutralState()
        {
            _profile.UpdateMapping(ControllerInput.A, ZwiftFunction.ShiftUp);

            await RunAsync(new ControllerData { A = true }, new ControllerData());

            var states = States();
            Assert.False(states[0].IsNeutral);
            Assert.True(states[1].IsNeutral);
        }

        [Fact]
        public async Task UnmappedInput_ProducesNoPressedFunctions()
        {
            await RunAsync(new ControllerData { B = true });
            Assert.True(States()[0].IsNeutral);
        }

        [Fact]
        public async Task RideOn_And_Navigation_ArePressedFunctions()
        {
            _profile.UpdateMapping(ControllerInput.X, ZwiftFunction.RideOn);
            _profile.UpdateMapping(ControllerInput.DPadUp, ZwiftFunction.NavigateUp);
            await RunAsync(new ControllerData { X = true, DPadUp = true });
            var s = States()[0];
            Assert.Contains(ZwiftFunction.RideOn, s.Pressed);
            Assert.Contains(ZwiftFunction.NavigateUp, s.Pressed);
        }

        [Fact]
        public void StopProcessing_SendsNeutral()
        {
            _sut.StartProcessing(Substitute.For<IController>());
            _sut.StopProcessing();
            _outputService.Received().UpdateAsync(Arg.Is<OutputState>(s => s.IsNeutral), Arg.Any<CancellationToken>());
        }
    }
}
