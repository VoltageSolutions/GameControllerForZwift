using GameControllerForZwift.Core;
using GameControllerForZwift.Network.Dircon;
using GameControllerForZwift.Network.Mdns;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class NetworkServiceTests
    {
        private readonly IZwiftControllerLink _link = Substitute.For<IZwiftControllerLink>();
        private readonly NetworkService _service;

        public NetworkServiceTests()
        {
            _service = new NetworkService(_link, new MdnsAdvertiser(), DeviceIdentity.Default, Substitute.For<ILogger<NetworkService>>());
        }

        [Fact]
        public async Task PerformActionAsync_Fails_WhenZwiftNotConnected()
        {
            _link.IsZwiftConnected.Returns(false);
            var result = await _service.PerformActionAsync(ZwiftFunction.ShiftUp);
            Assert.False(result.Success);
            await _link.DidNotReceiveWithAnyArgs().SendButtonsAsync(default);
        }

        [Fact]
        public async Task PerformActionAsync_Fails_ForUnmappedFunction()
        {
            _link.IsZwiftConnected.Returns(true);
            var result = await _service.PerformActionAsync(ZwiftFunction.HideHUD);
            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(ZwiftFunction.ShiftUp, ZwiftRideButton.RS1)]
        [InlineData(ZwiftFunction.ShiftDown, ZwiftRideButton.LS1)]
        [InlineData(ZwiftFunction.NavigateLeft, ZwiftRideButton.Left)]
        [InlineData(ZwiftFunction.Select, ZwiftRideButton.A)]
        public async Task PerformActionAsync_PressesThenReleasesMappedButton(ZwiftFunction function, ZwiftRideButton expected)
        {
            _link.IsZwiftConnected.Returns(true);

            var result = await _service.PerformActionAsync(function);
            Assert.True(result.Success);
            await Task.Delay(NetworkService.HoldTime + TimeSpan.FromMilliseconds(300));

            Received.InOrder(() =>
            {
                _link.SendButtonsAsync(expected, Arg.Any<CancellationToken>());
                _link.SendButtonsAsync(ZwiftRideButton.None, Arg.Any<CancellationToken>());
            });
        }

        private static ZwiftControllerState State(double steer = 0, double brake = 0, int? battery = null, ZwiftFunction[]? pressed = null)
            => new(new HashSet<ZwiftFunction>(pressed ?? Array.Empty<ZwiftFunction>()), steer, brake, battery);

        private NetworkService LoopService()
        {
            _link.IsZwiftConnected.Returns(true);
            return new NetworkService(_link, new MdnsAdvertiser(), DeviceIdentity.Default, Substitute.For<ILogger<NetworkService>>(), TimeSpan.FromMilliseconds(20));
        }

        private static async Task Settle() => await Task.Delay(150);

        [Theory]
        [InlineData(ZwiftFunction.ShiftUp, true)]
        [InlineData(ZwiftFunction.SteerLeft, true)]
        [InlineData(ZwiftFunction.SteerRight, true)]
        [InlineData(ZwiftFunction.Brake, true)]
        [InlineData(ZwiftFunction.HideHUD, false)]
        public void CanHandle_CoversButtonsAndAnalogAxes(ZwiftFunction function, bool expected)
            => Assert.Equal(expected, _service.CanHandle(function, ZwiftRiderAction.WaveHand));

        [Fact]
        public void CanHandle_RiderAction_OnlyRideOn()
        {
            Assert.True(_service.CanHandle(ZwiftFunction.RiderAction, ZwiftRiderAction.RideOn));
            Assert.False(_service.CanHandle(ZwiftFunction.RiderAction, ZwiftRiderAction.WaveHand));
        }

        [Fact]
        public void ButtonMap_NoLongerContainsSteering()
        {
            Assert.False(NetworkService.ButtonMap.ContainsKey(ZwiftFunction.SteerLeft));
            Assert.False(NetworkService.ButtonMap.ContainsKey(ZwiftFunction.SteerRight));
        }

        [Theory]
        [InlineData(-1.0, 0.0, -100, 0)]
        [InlineData(1.0, 0.0, 0, 100)]
        [InlineData(-0.5, 0.0, -50, 0)]
        [InlineData(0.25, 0.0, 0, 25)]
        [InlineData(0.0, 1.0, 100, -100)]
        [InlineData(0.0, 0.4, 40, -40)]
        [InlineData(-0.8, 0.3, -80, -30)]  // steer left wins ZL, brake still on ZR
        [InlineData(-0.2, 0.6, 60, -60)]   // brake wins ZL
        [InlineData(0.8, 0.3, 30, 80)]     // steer right wins ZR
        [InlineData(0.2, 0.6, 60, -60)]    // brake wins ZR
        public void PaddleMath_Computes(double steer, double brake, int zl, int zr)
        {
            var (l, r) = PaddleMath.Compute(steer, brake);
            Assert.Equal(zl, l);
            Assert.Equal(zr, r);
        }

        [Fact]
        public async Task UpdateAsync_SendsInputImmediately_ResendsWhileActive_ThenOneNeutral()
        {
            var svc = LoopService();
            svc.StartSendLoop();
            try
            {
                await svc.UpdateAsync(State(steer: -1, pressed: new[] { ZwiftFunction.ShiftUp }));
                await Settle();
                await _link.Received().SendInputAsync(ZwiftRideButton.RS1, -100, 0, Arg.Any<CancellationToken>());
                Assert.True(_link.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IZwiftControllerLink.SendInputAsync)) >= 3, "should resend while active");

                await svc.UpdateAsync(ZwiftControllerState.Neutral);
                await Settle();
                await _link.Received(1).SendInputAsync(ZwiftRideButton.None, 0, 0, Arg.Any<CancellationToken>());
                _link.ClearReceivedCalls();
                await Task.Delay(100);
                await _link.DidNotReceiveWithAnyArgs().SendInputAsync(default, default, default);
            }
            finally { await svc.StopSendLoopAsync(); }
        }

        [Fact]
        public async Task Loop_DoesNotSend_WhenZwiftNotConnected()
        {
            var svc = LoopService();
            _link.IsZwiftConnected.Returns(false);
            svc.StartSendLoop();
            try
            {
                await svc.UpdateAsync(State(brake: 1));
                await Settle();
                await _link.DidNotReceiveWithAnyArgs().SendInputAsync(default, default, default);
            }
            finally { await svc.StopSendLoopAsync(); }
        }

        [Fact]
        public async Task UpdateAsync_ForwardsBatteryOnlyOnChange()
        {
            await _service.UpdateAsync(State(battery: 80));
            await _service.UpdateAsync(State(battery: 80));
            await _service.UpdateAsync(State(battery: 55));
            _link.Received(1).SetBatteryPercent(80);
            _link.Received(1).SetBatteryPercent(55);
        }

        [Fact]
        public void CompositeOutputService_PrefersNetworkOnlyWhenConnected_ElseKeyboard()
        {
            var keyboard = Substitute.For<IOutputService>();
            var network = Substitute.For<ILanOutputService>();
            var options = new OutputOptions();
            var router = new CompositeOutputService(keyboard, network, options);
            network.Supports(Arg.Any<ZwiftFunction>(), Arg.Any<ZwiftRiderAction>()).Returns(c => c.Arg<ZwiftFunction>() is ZwiftFunction.ShiftUp or ZwiftFunction.Select or ZwiftFunction.Brake);
            network.CanHandle(Arg.Any<ZwiftFunction>(), Arg.Any<ZwiftRiderAction>()).Returns(c => c.Arg<ZwiftFunction>() is ZwiftFunction.ShiftUp or ZwiftFunction.Select or ZwiftFunction.Brake);

            // Zwift not connected: everything falls back to the keyboard (ShiftUp -> PageUp).
            network.IsAvailable.Returns(false);
            Assert.Same(keyboard, router.Route(ZwiftFunction.ShiftUp));
            Assert.Same(keyboard, router.Route(ZwiftFunction.Select));
            Assert.False(router.Handles(ZwiftFunction.ShiftUp, ZwiftRiderAction.WaveHand));

            // Connected: mapped functions go to the network, unmapped ones to the keyboard.
            network.IsAvailable.Returns(true);
            Assert.Same(network, router.Route(ZwiftFunction.ShiftUp));
            Assert.Same(network, router.Route(ZwiftFunction.Select));
            Assert.Same(keyboard, router.Route(ZwiftFunction.HideHUD));
            Assert.True(router.Handles(ZwiftFunction.Brake, ZwiftRiderAction.WaveHand));
            Assert.False(router.Handles(ZwiftFunction.HideHUD, ZwiftRiderAction.WaveHand));

            options.UseZwiftLanController = false;
            Assert.Same(keyboard, router.Route(ZwiftFunction.ShiftUp));
            Assert.False(router.Handles(ZwiftFunction.ShiftUp, ZwiftRiderAction.WaveHand));
        }
    }
}
