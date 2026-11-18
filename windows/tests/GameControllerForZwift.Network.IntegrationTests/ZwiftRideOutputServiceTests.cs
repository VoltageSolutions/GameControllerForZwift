using GameControllerForZwift.Core;
using GameControllerForZwift.Network.Output;
using GameControllerForZwift.Network.ZwiftRide;
using GameControllerForZwift.Network.Mdns;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class ZwiftRideOutputServiceTests : IAsyncDisposable
    {
        private readonly IZwiftRideLink _link = Substitute.For<IZwiftRideLink>();
        private readonly ZwiftRideOutputService _service;

        public ZwiftRideOutputServiceTests()
        {
            _service = new ZwiftRideOutputService(_link, new MdnsAdvertiser(), DeviceIdentity.Default, Substitute.For<ILogger<ZwiftRideOutputService>>());
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return _service.DisposeAsync();
        }

        private static OutputState State(double steer = 0, double brake = 0, int? battery = null, ZwiftFunction[]? pressed = null)
            => new(new HashSet<ZwiftFunction>(pressed ?? Array.Empty<ZwiftFunction>()), steer, brake, battery);

        private ZwiftRideOutputService LoopService()
        {
            _link.IsZwiftConnected.Returns(true);
            return new ZwiftRideOutputService(_link, new MdnsAdvertiser(), DeviceIdentity.Default, Substitute.For<ILogger<ZwiftRideOutputService>>(), TimeSpan.FromMilliseconds(20));
        }

        private static async Task Settle() => await Task.Delay(150);

        [Theory]
        [InlineData(ZwiftFunction.NavigateUp, ZwiftRideButton.Up)]
        [InlineData(ZwiftFunction.NavigateDown, ZwiftRideButton.Down)]
        [InlineData(ZwiftFunction.NavigateLeft, ZwiftRideButton.Left)]
        [InlineData(ZwiftFunction.NavigateRight, ZwiftRideButton.Right)]
        [InlineData(ZwiftFunction.Select, ZwiftRideButton.A)]
        [InlineData(ZwiftFunction.GoBack, ZwiftRideButton.B)]
        [InlineData(ZwiftFunction.Powerup, ZwiftRideButton.Y)]
        [InlineData(ZwiftFunction.RideOn, ZwiftRideButton.Z)]
        [InlineData(ZwiftFunction.ShiftUp, ZwiftRideButton.RS1)]
        [InlineData(ZwiftFunction.ShiftDown, ZwiftRideButton.LS1)]
        public void ButtonMap_MapsZwiftRideFunctionsToButtons(ZwiftFunction function, ZwiftRideButton expected)
            => Assert.Equal(expected, ZwiftRideOutputService.ButtonMap[function]);

        [Fact]
        public void ButtonMap_HasNoOtherEntries_AndNoNoneOrAnalog()
        {
            Assert.Equal(10, ZwiftRideOutputService.ButtonMap.Count);
            Assert.False(ZwiftRideOutputService.ButtonMap.ContainsKey(ZwiftFunction.None));
            Assert.False(ZwiftRideOutputService.ButtonMap.ContainsKey(ZwiftFunction.Brake));
        }

        [Fact]
        public async Task UpdateAsync_RideOnAndNavigation_SendButtonMask()
        {
            var svc = LoopService();
            svc.StartSendLoop();
            try
            {
                await svc.UpdateAsync(State(pressed: new[] { ZwiftFunction.RideOn, ZwiftFunction.NavigateUp }));
                await Settle();
                await _link.Received().SendInputAsync(ZwiftRideButton.Z | ZwiftRideButton.Up, 0, 0, Arg.Any<CancellationToken>());
            }
            finally { await svc.StopSendLoopAsync(); }
        }

        [Fact]
        public void ButtonMap_NoLongerContainsSteering()
        {
            Assert.False(ZwiftRideOutputService.ButtonMap.ContainsKey(ZwiftFunction.SteerLeft));
            Assert.False(ZwiftRideOutputService.ButtonMap.ContainsKey(ZwiftFunction.SteerRight));
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
                Assert.True(_link.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IZwiftRideLink.SendInputAsync)) >= 3, "should resend while active");

                await svc.UpdateAsync(OutputState.Neutral);
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
    }
}
