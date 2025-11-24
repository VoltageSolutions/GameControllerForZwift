using GameControllerForZwift.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace GameControllerForZwift.Network.IntegrationTests
{
    public class NetworkServiceTests
    {
        private readonly ILogger<NetworkService> _loggerMock;
        private readonly NetworkService _networkService;

        public NetworkServiceTests()
        {
            _loggerMock = Substitute.For<ILogger<NetworkService>>();
            _networkService = new NetworkService(_loggerMock);
        }

        [Fact]
        public async Task PerformActionAsync_PerformsCorrectKeyPress_ForZwiftFunction()
        {
            Thread.Sleep(10000);
            var result = await _networkService.PerformActionAsync(ZwiftFunction.NavigateRight);
            Thread.Sleep(1000);
            await _networkService.PerformActionAsync(ZwiftFunction.NavigateLeft);
            Thread.Sleep(1000);
            await _networkService.PerformActionAsync(ZwiftFunction.ShowMenu);
            Thread.Sleep(1000);
            await _networkService.PerformActionAsync(ZwiftFunction.Uturn);
            Assert.True(result.Success);
        }
    }
}
