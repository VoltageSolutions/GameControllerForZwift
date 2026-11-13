using NSubstitute;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Tests
{
    public class DataIntegratorZwiftOutputTests
    {
        private readonly IOutputService _output = Substitute.For<IOutputService>();
        private readonly IZwiftControllerOutput _zwift = Substitute.For<IZwiftControllerOutput>();
        private readonly IControllerProfileService _profileService = Substitute.For<IControllerProfileService>();
        private readonly ControllerProfile _profile = new ControllerProfile();
        private readonly DataIntegrator _sut;

        public DataIntegratorZwiftOutputTests()
        {
            var profiles = new ControllerProfiles();
            profiles.Profiles.Add(_profile);
            _profileService.Profiles.Returns(profiles);
            _zwift.Handles(Arg.Any<ZwiftFunction>(), Arg.Any<ZwiftRiderAction>()).Returns(true);
            _sut = new DataIntegrator(Substitute.For<IInputService>(), _output, _profileService, 10, _zwift);
        }

        private async Task RunAsync(params ControllerData[] data)
        {
            foreach (var d in data) _sut.ProcessControllerData(d);
            using var cts = new CancellationTokenSource(150);
            try { await _sut.WriteDataAsync(cts.Token); } catch (OperationCanceledException) { }
        }

        private ZwiftControllerState[] States() =>
            _zwift.ReceivedCalls().Where(c => c.GetMethodInfo().Name == nameof(IZwiftControllerOutput.UpdateAsync))
                  .Select(c => (ZwiftControllerState)c.GetArguments()[0]!).ToArray();

        [Fact]
        public async Task SteerRight_And_SteerLeft_ProduceSignedSteer()
        {
            _profile.UpdateMapping(ControllerInput.LeftThumbstick_TiltLeft, ZwiftFunction.SteerLeft);
            _profile.UpdateMapping(ControllerInput.LeftThumbstick_TiltRight, ZwiftFunction.SteerRight);

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
        public async Task UnhandledFunction_RoutesToOutputService()
        {
            _zwift.Handles(ZwiftFunction.Select, Arg.Any<ZwiftRiderAction>()).Returns(false);
            _profile.UpdateMapping(ControllerInput.B, ZwiftFunction.Select, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);

            await RunAsync(new ControllerData { B = true });

            await _output.Received().PerformActionAsync(ZwiftFunction.Select, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
            Assert.True(States()[0].IsNeutral);
        }

        [Fact]
        public async Task HandledFunction_IsNotSentToOutputService()
        {
            _profile.UpdateMapping(ControllerInput.A, ZwiftFunction.ShiftUp);
            await RunAsync(new ControllerData { A = true });
            await _output.DidNotReceive().PerformActionAsync(Arg.Any<ZwiftFunction>(), Arg.Any<ZwiftPlayerView>(), Arg.Any<ZwiftRiderAction>());
        }

        [Fact]
        public async Task HandledFunction_AlsoSentToOutputService_WhenKeyboardEnabled()
        {
            _zwift.SendsToKeyboard.Returns(true);
            _profile.UpdateMapping(ControllerInput.A, ZwiftFunction.ShiftUp, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
            await RunAsync(new ControllerData { A = true });
            await _output.Received().PerformActionAsync(ZwiftFunction.ShiftUp, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
            Assert.Contains(ZwiftFunction.ShiftUp, States()[0].Pressed);
        }

        [Fact]
        public async Task NoZwiftOutput_TriggerMappedFunctionGoesToOutputService()
        {
            var sut = new DataIntegrator(Substitute.For<IInputService>(), _output, _profileService);
            _profile.UpdateMapping(ControllerInput.LeftTrigger, ZwiftFunction.Powerup, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
            sut.ProcessControllerData(new ControllerData { LeftTrigger = 32767 });
            using var cts = new CancellationTokenSource(150);
            try { await sut.WriteDataAsync(cts.Token); } catch (OperationCanceledException) { }
            await _output.Received().PerformActionAsync(ZwiftFunction.Powerup, ZwiftPlayerView.Default, ZwiftRiderAction.RideOn);
        }

        [Fact]
        public void StopProcessing_SendsNeutral()
        {
            _sut.StartProcessing(Substitute.For<IController>());
            _sut.StopProcessing();
            _zwift.Received().UpdateAsync(Arg.Is<ZwiftControllerState>(s => s.IsNeutral), Arg.Any<CancellationToken>());
        }
    }
}
